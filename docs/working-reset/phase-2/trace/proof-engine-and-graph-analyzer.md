# Trace — the compiler proof documents

**Range**: `b93d1db1..HEAD`
**Files**: `docs/compiler/proof-engine.md`, `docs/compiler/graph-analyzer.md`, `docs/compiler/README.md`
**Hunks examined**: 47 (41 + 5 + 1, at three lines of context), grouped below into 24 changes

Line numbers given for a change are where it sits at HEAD. Line numbers for the want document are
`docs/Working/what-i-want-2026-07-16.md` at HEAD; for the walk, `docs/working-reset/phase-1/the-walk.md`.

## Summary

Three things were being done to these documents. The largest by volume was taking the runtime
fault-backstop machinery out of the proof engine's description — the `FaultSiteLink` records, the
`FaultSiteAnnotation` stamped on opcodes, the "defense-in-depth" reasoning that stood behind them — because
the owner's document says the runtime runs no fault-prevention check at evaluation time and never picks up
proof work the compiler could not finish. The second was correcting two places where the documents
described a proof model the owner's document contradicts: a section headed "No Inductive Proof", and a
claim that guard satisfiability waits on an SMT integration the compiler does not have and the
specification excludes. The third was housekeeping the documents needed on their own account — a strategy
count stated as six in some sections and five in others, prose numbering a strategy differently from its
own heading, and two canonical documents describing one diagnostic in opposite terms.

Of the 24 grouped changes: **13 rest on the owner's own text**, **2 rest on a ruling of his with his words
recorded** (both also have want-document support), **8 are internal repairs** — canon contradicting itself,
or contradicting the code, or a cross-reference pointing at a heading that had been renamed — and **3 carry
a part that traces to nothing he wrote or ruled**. That is 26 category assignments over 24 changes, because
two of the three category-4 parts sit on changes whose main substance is category 1 and are counted in both
places; those two entries say so where they sit.

The three consolidate into **two items on the short list**. The second of the two was recorded in an earlier
draft as a concern rather than as a category-4 item, and that was wrong: three sentences new to
`proof-engine.md` adopt a scope for ingress validation that the specification marks as disputed and
instructs documents not to acquire, and none of the three carries a disputed marking. It is the same
narrowing that the other trace documents file under category 4, so it belongs there here too.

**On the question the assignment flagged — does the want document actually require inductive proof.** It
does, in the owner's own words and at the top of the section that defines what the compiler promises.
Line 16: *"What \"prove\" means here is **proof by induction over reachable configurations** — not proof
that each rule is a static mathematical truth (a rule relating two runtime-supplied values has no static
truth to prove)."* Lines 18 and 19 then name both halves: *"**Base case.** The default configuration
satisfies every rule, and every initial event *establishes* every rule, provable from the constraints
declared on its args."* and *"**Inductive step.** Every handler *preserves* every rule, provable from four
premise classes: (a) field modifiers, (b) arg constraints, (c) the handler's guard, and (d) all rules
holding in the pre-state."* This is not an inference from scattered lines; it is the definition the whole
"compiler's promise" section is built on. The rewriting that rests on it is well founded.

---

## docs/compiler/proof-engine.md

### The fault-backstop machinery comes out (lines 8, 11, 18, 40, 51, 106, 120, 133, 262–274, 318–326, 380, 396, 1856–1880, 2082, 2153–2185, 2224, 2236–2280, 2329–2337, 2362–2376, 2496–2500, 2587–2592, 2632)

**What changed**: `FaultSiteLink`, `FaultSiteAnnotation`, the `ProofLedger.FaultSiteLinks` field, the
builder contract that planted `FaultSiteDescriptor` backstops on opcodes, and every sentence calling those
backstops defense-in-depth were removed from this document. The chain diagram that ran
`DiagnosticCode → FaultCode → FaultSiteLink → FaultSiteDescriptor` now ends at "Definition refused". Where
the old text said an unresolved obligation produces "both a diagnostic (for the author) and a
`FaultSiteLink` (for the Precept Builder)", it now says it produces a diagnostic and the definition does
not compile.

**Warrant**: category 1. Want line 165: *"But the runtime **can be lighter, because the compiler's work is
complete** … \"Lighter\" means concretely: **fault-prevention checks are omitted at evaluation time**. No
divide-by-zero tests, no overflow guards, no re-checking of proven bounds on writes — the evaluator just
computes."* Want line 171: *"In an ideal world there is **no deferral**. A business rule whose enforcement
the compiler cannot prove complete is rejected — never handed to the runtime on the compiler's own
initiative."* And want line 104, on the worked example: *"At runtime there is no zero-check anywhere — the
ingress validation of the arg is what makes the division safe, and the certificate records that
dependency."* A backstop planted at a site the compiler could not prove is exactly the runtime
fault-prevention check those three lines rule out, and it can only exist for a definition that compiled
with an undischarged obligation, which want:171 says does not happen.

### Two honesty notes added about the code (lines 8, 271–272)

**What changed**: the header's *Implementation state* row and a new blockquote under the `ProofLedger`
record both say that `src/Precept/Pipeline/ProofLedger.cs` still declares the `FaultSiteLinks` field, that
`ProofEngine.Diagnostics.cs` still populates it, that the language server still reads it, and that the
record printed above is the shape the ledger takes once that comes out.

**Warrant**: category 3, internal repair — the document would otherwise print a record shape the code
contradicts. Verified: `src/Precept/Pipeline/ProofLedger.cs:8` declares
`ImmutableArray<FaultSiteLink> FaultSiteLinks`, and `:117` and `:124` declare `FaultSiteLink` and
`FaultSiteAnnotation`. The note is accurate.

### "No Inductive Proof" is replaced (lines 82, 2607–2618)

**What changed**: the §14 exclusion headed **No Inductive Proof** — *"The proof engine does not handle loop
invariants, recursive definitions, or inductive properties. Precept does not have loops or recursion"* —
becomes **No Loop Invariants, No Recursive Definitions**. The two genuine exclusions and their reason are
kept. Two paragraphs are added: one stating that proof over the reachable configurations is an induction
and is the model this engine serves, with the base case, the inductive step and the four premise classes
spelled out; one naming two places where the engine does not do that today, so the model is not read as a
description of the build.

**Warrant**: category 1, and this is the single most important warrant in the group. Want line 16:
*"What \"prove\" means here is **proof by induction over reachable configurations**"*. Want lines 18–19
give the base case and the inductive step verbatim, and want line 20 gives the third piece the added
paragraph uses: *"**Symmetric obligations.** A rule's obligations attach to every write site of *every*
field the rule mentions — not just the field the author was thinking about."* A document heading that says
the engine excludes inductive properties denies the model the owner's document defines. The narrower
exclusions the section keeps are true and are not in tension with anything: Precept has no loops and no
recursion.

The two gaps the second added paragraph names both check out. The first — that a rule holding in the
pre-state is available as a premise only where a declared rule contributes an unconditional fact — matches
walk item 30, which the owner marked *mine on the substance*: *"A rule can be consumed as a proof premise
while nothing establishes or preserves it — a confirmed fail-open defect, not intended behaviour."* The
second — that the enumeration of write sites is asserted by the specification rather than kept closed by
the build — is stated by `precept-language-spec.md:2230` in its own Open Questions section, which calls it
*"a soundness dependency, not a documentation nicety"*.

### The certificate checker becomes in-scope rather than deferred (line 2536)

**What changed**: the §13 bullet headed *"Certificate format only — no live re-checker in the MVP"* becomes
*"Certificate format, and the checker that replays it"*. The checker is described at both of its call sites
— a compiler stage auditing the engine at authoring time, and a load-time gate in the runtime that refuses
to govern under a definition whose certificate does not verify. That none of it is built is stated
plainly, and named as a gap against the target rather than a deferral of it.

**Warrant**: category 1. Want lines 201–204: *"One small checker component, two call sites: — **A compiler
stage** — audits the proof engine's work at authoring time, catching proof-engine bugs when they are
cheapest to fix. — **A load-time gate in the runtime** — the same checker, run once when a definition is
loaded. The runtime refuses to govern under a definition whose certificate does not verify: no unproven,
tampered, or stale definition can ever govern."* Want line 206 supplies the reason the added text gives for
the checker staying small: *"The hard work — figuring out *how* to prove each obligation — happens once, at
compile time. The certificate records the finished proof step by step, and the checker simply walks those
steps."* The plan's § 3 also carries a row saying the checker is in scope including its runtime half, but
that row is agent-written with no quotation attached, so the want lines are the warrant, not it.

### The rule-vs-default fold gets a three-way verdict, and its suppression is named as too wide (lines 443, 1935, 2020–2024)

**What changed**: three places said the default-value folds reject only on a fold that proves `false`, and
treat an unresolvable fold as satisfiable. All three now state a three-way verdict — an unresolvable fold
rejects too — and each says the code currently under-emits against that. Separately, the whole-precept
suppression that switches the rule-vs-default fold off entirely when a construction event exists is
renamed from a conservative under-emit to an implementation gap wider than the exception the language
allows.

**Warrant**: category 2 and category 1 together. The ruling is walk item 38, marked ☑ mine on 2026-07-26:
he was *"asked plainly whether both non-proven outcomes stop compilation, including \"couldn't work it
out\"; his answer: \"yes\"."* The want lines behind it are 171 (*"A business rule whose enforcement the
compiler cannot prove complete is rejected"*) and 18, which puts the defaults inside the proof at all
(*"The default configuration satisfies every rule"*), plus the must-not-compile row at want:124 —
*"Change the default to `20000.0` | Default value violates `max 10000.0` — the base case has a
counterexample"* — under a table the document introduces at line 110 by saying each row *"**must be a
compile-time rejection**"*.

One thing to be straight about: the walk ruling was put to him about containment obligations on writes.
Extending it to a default that cannot be folded is the editor's step, though want:18 and want:171 support
it independently. The specification's §0.6 item 11 was itself rewritten in this same phase to say the same
thing — at `b93d1db1` it read *"Only a provably-false fold rejects; an unknown/unfoldable default never
rejects"* — so citing the specification here is citing a sibling change, not independent support. The
owner's own lines and the walk ruling are what carry it.

### The count and length strategies are re-grounded on the premise classes (lines 1673, 1691, 1693)

**What changed**: three sentences that explained why a bound proof holds. The length strategy's *"The proof
works by the operand *carrying* a declared constraint"* becomes *"The premise is the modifier the author
declared on the source … premise classes (a) and (b)"*. The count strategy's *"the runtime count-fault trap
is a defense-in-depth backstop, unreachable for contract data"* becomes *"nothing catches an over-count
later … Either this obligation discharges here or the definition is refused."* And the count interval's
seeding, previously justified by *"§3A.4 swept prior state"*, is now justified by the pre-state premise the
induction supplies.

**Warrant**: category 1. Want line 19 names the four premise classes the new wording uses, including
*"(d) all rules holding in the pre-state"*, which is exactly what licenses seeding the interval from the
governed band. Want line 22 licenses treating a `mincount`/`maxcount` bound as a rule: *"**Modifiers are
sugar for rules.** … There is one constraint mechanism underneath — the premise classes above are
spellings, not separate systems."* Want line 165 removes the fault trap the old sentence leaned on. The
proof mechanics themselves — the deltas table, the guard narrowing — are untouched.

### The ConstraintInfluenceMap stops being a re-check index (lines 2170–2174, 2185)

**What changed**: *"given a field or arg that changed, which constraints might be affected? Used by the
evaluator for constraint re-check"* becomes an index of which constraints mention a field or arg, read at
ingress. A short paragraph says outright that it is not a re-check index, because the runtime does not
re-evaluate rules against a changed configuration. The field table's runtime-artifact column changes to
match.

**Warrant**: category 1. Want line 165 forbids the re-check the old description named:
*"no re-checking of proven bounds on writes — the evaluator just computes."* Want line 210 gives the
positive account the new text uses: *"the premise list **is** the runtime's validation surface — which
guards and constraints must run and can never be skipped — derived from the proof, never maintained in
parallel."* Walk item 16 also bears on it: shown the canon sentence that had a relational rule over
independently-set fields governed at runtime, his words were *"that was derived, I did not write that
exactly"*, and the alternative he then chose was that the compiler refuses the definition rather than the
runtime evaluating the rule on every operation.

**And category 4 for one phrase in the replacement.** The new text at `:2170` describes the index as routing
an entering value *"to the constraints declared on the slot it enters"* — the disputed narrowing, carried
with no disputed marking. See item 2 of the short list.

### "No Constraint Evaluation" is retitled and rewritten (lines 80, 2593–2597, and the § 3 Out of Scope row at line 133)

**What changed**: the §14 exclusion said proof and evaluation split as *"Proof engine: can this constraint
ever be satisfied given the structure? Evaluator: is this constraint satisfied right now given these
values?"* — which describes the evaluator re-checking rules against values. It is retitled **No Evaluation
of Rules Against Values** and the two questions are rewritten. The § 3 Out of Scope row *"Runtime
constraint checking is the evaluator's responsibility"* was rewritten the same way.

**Warrant**: category 1 for what was removed — want:165 and want:171 as above. **Category 4 for part of
what was written in its place**: both `:2597` and the § 3 row at `:133` state the disputed narrowing, and
neither carries a disputed marking. See item 2 of the short list.

### The trade-off accepted under Decision 1 is restated (line 2362)

**What changed**: the third reason the bounded strategy set was acceptable — *"Defense-in-depth backstops
catch any runtime failures"* — is replaced by the opposite admission: an obligation no admissible strategy
can discharge refuses the definition, and *"a program that is in fact safe, but safe for a reason no
strategy can certify, does not compile."*

**Warrant**: category 1. Want line 189: *"Weakening the guarantee is dilution — constructs that compile
without their obligations proven, checks silently deferred, warnings where there should be errors. The
guarantee must be exactly that: a guarantee."* Want line 185: *"I'm happy to **trade off some
expressibility to gain proof**"*. The cost the new sentence states is the cost the owner's document says it
is taking on.

### Decision 2's "what crosses the boundary" (lines 2368–2374)

**What changed**: *"Only `FaultSiteAnnotation` records … cross into runtime"* becomes *"Nothing from it
crosses into runtime"*, and a sentence is added noting that the constraint execution plans the runtime
model carries reach it by a different route and are not produced by the proof engine.

**Warrant**: category 1 for the removal (as above). The added sentence about execution plans is category 3,
an internal repair — the old bullet list put execution plans and fault annotations under one heading of
things that cross, so removing one would have left the other looking like a contradiction.

### The strategy count is replaced by a pointer to the enumeration (lines 91, 106, 120, 148–152, 154–158, 2329, 2337, 2504–2509, 2529, 2547, 2565, 2567, 2601, 2620)

**What changed**: every bare numeral counting the strategies — "six strategies only", "Strategy count | 6",
"Why six strategies", "one of six strategies succeeded", "Five strategies only", "Adding a sixth strategy",
"Five-strategy coverage validation", "the five-strategy boundary", "The six strategies are local and
shallow", "The five-strategy set is sufficient" — becomes a pointer to § Proof Strategies. A note at § 4
records the disagreement openly and picks no number.

**Warrant**: category 3, internal repair, and a plain one: the document said six in some sections and five
in others about the same set. The note also records a second in-document disagreement that is real —
verified at HEAD, the `ProofStrategy` enum has `CompositionalConstraint = 6`, `IntervalContainment = 7`,
`LengthContainment = 8`, `CountContainment = 9`, `DimensionalProduct = 10`, while the section headings run
Dimensional Product at 6, Compositional Constraint at 7, and Interval, Length and Count at 8, 9 and 10. The
note's claim that `compiler-and-runtime-design.md` used to say four and now states no count also checks
out: at `b93d1db1` that document's line 598 read *"four strategies only"*, and at HEAD its line 165 says
*"a bounded, non-extensible set of proof strategies"* with no number.

### Prose numbering of the Compositional Constraint strategy corrected (lines 1448, 1471, 1484, 1493)

**What changed**: five references to "Strategy 6" inside the Compositional Constraint section become
"Strategy 7".

**Warrant**: category 3, internal repair. The section's own heading was already `#### Strategy 7:
Compositional Constraint Proof` at `b93d1db1`, and `#### Strategy 6: Dimensional Product Proof` sat
immediately above it, so the prose was calling itself by its neighbour's number.

### The Roslyn analyzer description is corrected against the analyzers (lines 1872, 2496)

**What changed**: *"Roslyn analyzers (PRECEPT0001, PRECEPT0002) enforce that every evaluator failure path
routes through a classified `FaultCode`"* becomes a statement of what the two analyzers check — that every
`Fail()` call classifies with a `FaultCode`, and that every `FaultCode` member names a valid
`DiagnosticCode`.

**Warrant**: category 3, internal repair against the code. The two analyzer files are
`src/Precept.Analyzers/Precept0001FailMustUseFaultCode.cs` and
`Precept0002FaultCodeMustHaveStaticallyPreventable.cs`; their names say what they check, and neither walks
evaluator failure paths.

### The `[StaticallyPreventable]` map loses one of its two policy exceptions (line 2500)

**What changed**: the sentence naming *"Two pieces of policy remain explicit code"* — the collection-safety
many-to-one collapse, and *"the conservative backstop for proof-only obligation families with no
representable runtime fault of their own"* — becomes one piece, the collapse.

**Warrant**: category 1, inherited from the fault-backstop removal above (want:165, want:171). The dropped
exception existed only to decide what backstop to plant where no fault code fitted; with nothing planted
there is nothing for it to decide.

### The reference-identity invariant on `ProofObligation.Site` is kept and re-argued (lines 318–326)

**What changed**: the CC#6 resolution blockquote — which explained the invariant by the builder matching
`ProofLedger.FaultSiteLinks` by object identity during Pass 4, with the matching code and the annotation
shape — is cut down to the invariant itself, plus a new "Why" paragraph and a new paragraph saying it is
not settled whether any consumer actually needs the distinction, and that the certificate checker's
specification is where the question closes.

**Warrant**: split. Deleting the matching mechanism is category 1, inherited from the fault-backstop
removal — the consumer it described no longer exists. **The added "what is not settled" paragraph is
category 4** and is written up in the short list below.

### Diagnostic-code selection loses its second reader (line 2224)

**What changed**: *"the same catalog field `CreateFaultSiteLink` reads — so the code lives in exactly one
place"* becomes *"which is the one place the code lives — `CreateDiagnostic` is its only reader"*.

**Warrant**: category 1, inherited — `CreateFaultSiteLink` was removed by the fault-backstop change, so a
sentence naming it as the second reader would have pointed at nothing.

### Collection non-empty projection stops mapping to a runtime fault (line 2082)

**What changed**: *"emit `UnguardedCollectionAccess` and map to `FaultCode.CollectionEmptyOnAccess`"*
becomes a statement of which diagnostic is emitted, with the fault classes named as a build-time pairing in
the catalog rather than something the diagnostic carries into runtime.

**Warrant**: category 1, inherited from the fault-backstop removal.

### The severity sentence in Invariant 3 is corrected (line 2461)

**What changed**: the parenthetical *"(the contradictory/vacuous/unsatisfiable-rule family stay `Warning`
per §0.6 items 7/8 \"reports\")"* is replaced by a sentence saying the invariant does not depend on those
severities, and stating them correctly: contradictory and unsatisfiable rules reject, a vacuous rule stays
a report.

**Warrant**: category 2 and category 3 both. The ruling is walk item 22, marked ☑ mine on 2026-07-26 —
*"his words: \"I ruled that these should be elevated to error\""* — covering required-state dominance,
unsatisfiable rule and contradictory rule. The internal repair is independent of the ruling and predates
this phase: `precept-language-spec.md:209` at `b93d1db1` already said at §0.6 item 7 that an uninhabitable
rule set means *"the compiler **rejects the definition**"*, so citing items 7 and 8 together for a single
`Warning` disposition misread item 7.

### The sample-file coverage validation item is rewritten (line 2547)

**What changed**: *"validate five-strategy coverage against all 20 sample files in `samples/` before
committing to no sixth strategy"* becomes strategy coverage validation measured against the language
documented in `docs/language/`, with a parenthetical saying counting sample files establishes nothing.

**Warrant**: category 3, internal repair, on two counts. `CLAUDE.md` § *The samples are examples, not a
specification* forbids exactly this measurement — *"Never count sample files to establish coverage"* — and
the number was wrong anyway: there are 78 `.precept` files in `samples/`, not 20.

### Housekeeping that follows other changes (lines 40, 51, 78, 80, 82, 2632, and the §13 wording at 2565 and 2567)

**What changed**: table-of-contents entries renamed to match renamed headings; the "Fault Chain Integrity"
contract removed along with the chain it described; the cross-reference to *FaultSiteDescriptor planting*
in `precept-builder.md` removed; "a sixth proof strategy" and "the five-strategy boundary" in the Future
Considerations list reworded.

**Warrant**: mechanical consequences. The heading renames and the removed contract inherit the induction
change (category 1) and the fault-backstop change (category 1); the wording in Future Considerations
inherits the strategy-count repair (category 3). One entry is its own small repair, category 3: the
table-of-contents line for *No SMT Solver* pointed at a heading that had been renamed to *No Opaque
Solver* in a commit before this range, so the anchor was dead.

---

## docs/compiler/graph-analyzer.md

### Guard satisfiability no longer waits on an SMT integration (line 792)

**What changed**: the Deliberate Exclusions row read *"Guards contain arbitrary expressions. Determining
whether a guard can be satisfied requires the proof engine's SMT integration, not graph-level analysis."*
The boundary claim is kept — it is not graph-level analysis — and the mechanism claim is replaced: guard
satisfiability is decided by the proof engine's satisfiability scan, from the same bounded strategy set as
every other obligation, with no solver to wait for.

**Warrant**: category 3, internal repair, with category 1 support. The two conflicting statements: this row
said guard satisfiability *requires* the proof engine's SMT integration, while `proof-engine.md` § Two-Pass
Design has described a satisfiability scan detecting *"provably-empty guards, tautological guards"* in
`ProofEngine.Satisfiability.cs` since before this range, and `proof-engine.md` § 4 has always listed
external dependencies as zero, no SMT solver. So one document said the work waits on a component the other
said does not exist and is not wanted.

The want document bears on it at line 135, on non-linear rules: *"It is never governed by a post-mutation
sweep, and never deferred to a solver the compiler doesn't carry."* That line is cited as the want
document's, not quoted as the owner's own words: it is one of the sixteen lines the 2026-07-19 amendment
typed, and it is written in the first person although he did not write it. The content carries his
approval — he reviewed the document, amendment included — so it supports the change; the phrasing is not
his to quote. `language-documents.md` § *Raised and withdrawn* has the record. The specification
citations the new row adds also check out and both predate this range: `precept-language-spec.md` §0.6
items 9 and 10 are dead-guard and tautological-guard detection, and §0.8 item 4 says *"SMT-style or
theorem-prover-style proof artifacts (which would be opaque to the audience) are excluded."*

One qualification worth recording, because a later reader may trip on it. The new row's phrasing — *"there
is no solver in the compiler to wait for"* — is true of the compiler as it stands, but §0.6 Proof
philosophy #3 in the same specification says the *search* procedure is unconstrained and that the
legibility criterion *"replaces an earlier outright ban on solver-style engines"*. Read as a permanent
prohibition the sentence would overstate what §0.6 allows; read as a description of today's compiler it is
accurate. It is not wrong as written, but it is one word away from being wrong.

### Dead-end states become an Error (lines 271, 452, 457–459, 779)

**What changed**: four places. The pseudocode emits `Diagnostic(DeadEndState, Error)`; the § 6.2 severity
paragraph — previously *"Warning, not error. Dead-end states may be intentional in designs that use certain
states as permanent holds … The author can suppress the warning by adding the `terminal` modifier"* —
becomes Error with the `terminal` modifier described as the author's declaration rather than as warning
suppression; the fact-table row and the resolved-question note at § 12 are brought into line, the latter
recording what it used to say and why that reasoning does not survive. A new paragraph records that
`Diagnostics.cs` still emits Warning.

**Warrant**: category 1, and it is close to verbatim. Want line 152: *"**No dead ends.** Every non-terminal
state has an exit — `Frozen` exits via `Unfreeze`. A state you can enter but never leave is rejected,
unless the author deliberately marked it `terminal` (`Closed`): an end on purpose is a declaration, not a
defect."* The old paragraph's reasoning — that a permanent hold may be intentional and the warning can be
suppressed — is the position that line rejects, and the new paragraph's reasoning is that line's reasoning.
Want line 126 puts it in the must-not-compile table as well: *"Delete the `Unfreeze` rows | `Frozen` is a
dead end — enterable, not exitable, not marked `terminal`"*, under the introduction at want:110 that each
row *"**must be a compile-time rejection**"*.

Note for the record that this is **not** one of the three severity flips the owner ruled at walk item 22 —
those were required-state dominance, unsatisfiable rule and contradictory rule. Dead-end state stands on
the want document directly, not on that ruling.

The implementation-gap note is accurate: `src/Precept/Language/Diagnostics.cs:726` still declares
`DeadEndState` with `Severity.Warning`.

### The zero-coverage `UnhandledEvent` characterisation (line 538)

**What changed**: *"**Zero coverage** … — provably dead declaration. Emit `UnhandledEvent` (Warning)"*
becomes a statement that no state accepts the event, that the code reports rather than blocks, and why.
The severity does not change.

**Warrant**: category 3, internal repair. The two conflicting statements: this line called a
zero-coverage event a *"provably dead declaration"*, while `diagnostic-system.md` § Severity said a
*"deliberately-forbidden event"* is a *"supported design, not an error"*. Two canonical documents describing
one diagnostic in opposite terms, with the risk that a reader meeting this one first treats the Warning as
unfinished business and flips it.

---

## docs/compiler/README.md

### The soundness document's row (line 37)

**What changed**: *"the deferred independent re-checker"* becomes *"the independent checker"* in the
one-line description of `soundness-and-coverage.md`.

**Warrant**: category 1, inherited from the certificate-checker change above — want lines 201–204. This is
the only hunk in the file.

---

## Traces to nothing he wrote or ruled

> **Dispositioned 2026-08-26 — both items are closed.** Item 1's paragraph **came out of `proof-engine.md`**
> on his *"lets go clean"*; the invariant and the "Why" paragraph above it stay, and phase 5's certificate
> checker meets the question on its own. Item 2's three sentences **came out of canon** along with every
> other site of the "and nothing wider" narrowing — he did not want the underlying question settled in
> passing. What a direct field edit evaluates is still open and still belongs to phase 5. The two findings
> below are left as they were written and describe the file before that surgery. His words and his reasons:
> [`../trace-walk-rulings.md`](../trace-walk-rulings.md).

Two items. The first is small and nothing rests on it. The second is a sentence that took a side in a
question still going to the owner, in three places, unmarked.

### 1. `proof-engine.md:324–326` — the paragraph saying the reference-identity invariant may not be needed

**Before** (part of a longer CC#6 blockquote that also carried the matching code, all of which was removed
with the fault backstops):

> **Invariant:** The proof engine MUST NOT use `with { ... }` on `obligation.Site` or create new
> `TypedExpression` instances — it must preserve the original object reference. … Reference identity
> prevents false positives from structural equality matching identical-but-distinct expressions in
> different transition rows.

**After** (the invariant is kept; these two paragraphs are new):

> **Why:** reference identity is the only thing that tells two identical-but-distinct expressions apart.
> The same text written into two transition rows produces two `TypedExpression` values that compare equal
> structurally, so a consumer matching on structure attributes an obligation to whichever of them it meets
> first. Copying the `Site` with `with { }` discards the distinction.
>
> **What is not settled is whether any consumer needs the distinction.** Diagnostic emission matches on the
> span, and so does the language server when it looks for the obligation behind a squiggle. Until something
> has to separate two structurally identical sites, the invariant costs nothing and buys nothing, and the
> argument above is a reason to keep it rather than a demonstration that anything would break without it.
> The specification of the certificate checker is where this closes: a checker that replays a discharge has
> to find the expression the discharge was about, and how it identifies that expression decides whether
> `Site` must be a reference or may be a span.

**What was searched for and not found**: the want document says nothing about expression identity,
`TypedExpression`, spans, or how a certificate locates the expression a discharge was about — searched for
*identity*, *reference*, *span*, *expression* and read the certificate section at want:197–213, which
specifies what a certificate contains (the theorem, the premises used, the derivation, and the author
declaration each premise cites) but not how a step names its site. The walk has no item on it. The plan
records it as an open question owned by phase 5, but that is the plan's own wording with no words of his
attached, which the brief's rules say is an agent artifact rather than authority. The first of the two
paragraphs is defensible as replacing a justification the fault-backstop removal took away; the second is
new analysis and a new open question, written by the editor.

This is a small addition and nothing rests on it. It is listed because the alternative — filing it under
"internal repair" — would be stretching that category to cover an editor introducing a question rather than
resolving a contradiction.

---

### 2. Three new sentences in `proof-engine.md` adopt a scope for ingress validation that canon marks as disputed

**Where**: `proof-engine.md:133`, `:2170` and `:2597`. Canon is not edited from here, so the three sentences
were still in the file exactly as described when this was written. They came out on 2026-08-26 — see the
note at the head of this section.

`proof-engine.md:133`, `:2170` and `:2597` now each describe ingress validation as holding a value to *"the
constraints declared on the slot it enters"*, and `:2597` adds *"and nothing beyond them"*. None of these
three phrasings existed in the file at `b93d1db1` — the sentences they replaced described the evaluator
re-checking constraints, which is the thing want:165 and want:171 rightly remove.

The problem is what went in. `precept-language-spec.md:276–288` carries an explicit warning box on that
exact wording:

> **This scope is disputed and is not settled canon.** The words "and nothing wider" narrow ingress to the
> constraints declared on the slot a value enters. `docs/Working/what-i-want-2026-07-16.md:143` says the
> opposite for an editable-field write … **Do not build against this scope, and do not cite it as
> evidence.**

The owner's line it contradicts, want:143: *"For an editable-field write, it's the field's modifier-rules
*and* every rule that mentions the field — an edit is an arbitrary incoming value that no static proof can
cover, so evaluating those rules at the ingress point is exactly the premise that closes the editable write
sites in the compiler's proof."*

**The rule this breaks, and what it required.** The same phase's own decision sheet for the fault-backstop
work (`phase-2/fault-backstop-model.md` § 1) set it: *"Every document that already states the narrow scope
keeps its wording *and* its disputed marking, untouched. No document that does not already state it acquires
one."* So the rule required two things of `proof-engine.md`, and neither happened. It was not a document
that already stated the narrow scope, so under the rule it should not have acquired the wording at all. And
every canonical statement of the narrowing that the rule left standing carries a disputed marking within a
line or two of it — eight of them: `precept-language-spec.md:274` and `:1992`,
`compiler-and-runtime-design.md:137` and `:151`, `evaluator.md:738` and `:1381`, `result-types.md:152` and
`runtime-api.md:402`, with two further marked entries in the open-question indexes at `evaluator.md:2015`
and `runtime/README.md:41`. `proof-engine.md`'s three sites carry nothing:
`grep -c "disputed" docs/compiler/proof-engine.md` returns **0**. A reader of `proof-engine.md` alone has no
way to know the scope is contested.

The removal these sentences were part of is warranted. The replacement wording takes a side in a question
the plan lists as still going to the owner ("What discharges a rule's obligation when a field it mentions
is edited?"), and it does so in the document that describes the proof the answer would change. That is what
puts it in this list rather than under internal repairs: it is the same "and nothing wider" narrowing that
`language-documents.md`, `philosophy-and-architecture.md`, `runtime-outcomes.md` and `evaluator.md` all file
as category 4, and the only difference here is that these three sites carry it unmarked.
