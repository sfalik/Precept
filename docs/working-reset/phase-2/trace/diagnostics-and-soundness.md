# Trace — the diagnostic system and the soundness document

**Range**: `b93d1db1..HEAD` **Files**: `docs/compiler/diagnostic-system.md`,
`docs/compiler/soundness-and-coverage.md` **Hunks examined**: 44 changed regions at zero context
(33 in the diagnostic document, 11 in the soundness document; 128 changed lines in the first file, 85 in
the second), grouped below into 28 changes.

Four commits touched these two files: `c3a9be21` (one region), `d32d8e26` (the compiler-pipeline pass),
`fa4891d7` (the MVP wording), and `c9193ec8` (removal of the model in which the runtime re-checks what the
compiler proved). Every region was read from the diff first; the working papers and the register were
consulted afterwards, and only to find where the reasoning had been written down.

## Summary

The two documents were changed for three reasons. Most of the volume — and almost all of the deletion — is
the removal of the model in which a compiled definition still carries fault checks the evaluator runs, and
that traces to a line the owner wrote himself: want:165, "**fault-prevention checks are omitted at
evaluation time**. No divide-by-zero tests, no overflow guards, no re-checking of proven bounds on writes —
the evaluator just computes." Second, several statements were re-grounded on his one-constraint-mechanism
sentence at want:22, which makes a declared bound a rule rather than a fault gate. Third, the certificate
checker was restored to scope at both of the call sites want:201–204 describes, and the three verdicts a
refusal can carry were made author-visible on the ruling he gave at the 2026-07-26 walk, item 11.

Of the 28 changes: **23 rest on the owner's own text** in `what-i-want-2026-07-16.md` (category 1), **1
rests on a ruling he gave** with his words quoted (category 2), **4 are internal repairs** (category 3),
and **none falls into category 4**. Two of the category-1 changes (S3, S5) also have a quoted ruling
standing behind them, and three (D11, D12, D16) carry a small category-3 part alongside the category-1
part; each is said so in its entry. Two of the four internal repairs add canonical text that goes beyond
repairing a contradiction; both are quoted in full at the end so he can read them without opening the diff.

**On the ruling the brief asks about.** The 2026-07-26 walk item 11 ruling — a refusal reports the search,
and the compiler's errors must be honest about which kind of refusal they are — is **implemented** by one
change here (`diagnostic-system.md:183`, change D6) and contradicted by nothing in the diff. One
pre-existing paragraph the diff did not rewrite now sits in tension with it; that is recorded under
Observations, not as a change.

---

## `docs/compiler/diagnostic-system.md`

### D1 — table-of-contents entry removed (was line 35)

**What changed**: the entry "Evaluator usage" is gone.
**Warrant**: category 1, inherited. Mechanical consequence of D15, which deletes the section it pointed at;
D15's warrant is want:165.

### D2 — what the `[StaticallyPreventable]` attribute is (line 72)

**What changed**: "the runtime-to-compiler linkage" becomes "the pairing of each fault class with the
diagnostic that prevents it, checked when the project is built".
**Warrant**: category 1. want:165 — "**fault-prevention checks are omitted at evaluation time**. No
divide-by-zero tests, no overflow guards, no re-checking of proven bounds on writes — the evaluator just
computes." There is no runtime end for the attribute to link to, so what remains is a pairing the build
checks.

### D3 — the `FaultCode` input and the `Fault` output (lines 95, 102)

**What changed**: "evaluator failure paths call `Fail(FaultCode)`" becomes "each member names a class of
failure and, via `[StaticallyPreventable]`, the `DiagnosticCode` member responsible for preventing it".
"`Fault` output records (from the evaluator)" becomes "`Fault` output records … Nothing constructs one
today (`fault-system.md` Q3)".
**Warrant**: category 1 for the deletion of the evaluator as producer — want:165 again. The replacement
wording is also what the later ruling requires: on 2026-08-11 he deferred the question of who, if anyone,
constructs a `Fault` — his words, recorded in the plan's § 4: *"do we need to decide this now? seems like
something we can defer until we start building the runtime"* — so canon records the question as open
rather than answering it, which is what "Nothing constructs one today (`fault-system.md` Q3)" does.

### D4 — "value-safety" codes become "containment" codes (line 162)

**What changed**: "The declared-bound **value-safety** codes are `Proof`" becomes "The declared-bound
**containment** codes are `Proof`. These are rule-preservation obligations, not fault gates — a bound is
rule shorthand — and `NumericOverflow` names a computed value leaving a declared bound, not arithmetic
overflow of the machine type."
**Warrant**: category 1. want:22 — "**Modifiers are sugar for rules.** `nonnegative` on `OverdraftLimit` is
a compact spelling of `rule OverdraftLimit >= 0` … There is one constraint mechanism underneath — the
premise classes above are spellings, not separate systems." He confirmed the line is his at the walk,
item 34: "the quoted line is mine".

### D5 — the severity axis, and the two kinds of operation that carry a containment obligation (lines 179, 181)

**What changed**: the axis is renamed from "fault-prevention obligations" to "containment obligations", and
the one list of fault-prone operations ("division, overflow, non-negative, out-of-range, bounded write") is
split into a fault-prone operation and a bounded write, with the bounded write named as rule preservation
rather than a fault. The disposition of both is unchanged: both still block.
**Warrant**: category 1, the same want:22 sentence as D4.

### D6 — new paragraph: the three verdicts are author-visible (line 183)

**What changed**: a paragraph is added stating that *proven*, *proven-violating* and *unresolved* are not
internal classification vocabulary — the distinction has to reach the message the author reads — while both
non-proven verdicts still reject, so nothing compiles under a weaker promise.
**Warrant**: category 2. The 2026-07-26 ruling at walk item 11, his words, recorded in the plan's § 4 and
in [`../../phase-4/what-a-refusal-claims.md`](../../phase-4/what-a-refusal-claims.md) § Ruled: *"option one
— we are going with a minimal proof set for the MVP, but we can and will expand the compiler's proof power
over time"*, with the requirement attached: *"What I would like, ideally, is to be honest in our compiler
errors — proven safe, proven unsafe, unproven or something like that."* The paragraph's guard against
misreading — that the distinction is about why a definition was refused, never about how strongly an
accepted rule is held — is want:173: "no tiers of trustworthiness to reason about."

### D7 — `UnsatisfiableGuard` moves from report to blocking (lines 189, 191, 193)

**What changed**: `UnsatisfiableGuard` leaves the list of advisory reports and becomes its own blocking
sub-family, "a declared path that can never be taken"; the report sentence now says why `VacuousRule` and
`TautologicalGuard` stay reports; and `UnsatisfiableGuard` is added to the list of codes whose current
`Severity.Warning` in `Diagnostics.cs` is tracked drift.
**Warrant**: category 1. want:155 — "**Dead rows are structural defects too.** A row whose guard can never
be true given the declared constraints is unreachable — and whatever state it leads to may be unreachable
in consequence. The ideal compiler rejects it with an explanation (e.g., a guard comparing a 0-to-1 ratio
against `75` instead of `0.75` can never fire — and the total-loss path it guards silently never happens)."
The document's new text uses that same worked case. The tracked-drift addition at line 193 is a mechanical
consequence of the same change.

### D8 — the PRE0164 default fold becomes three-way (line 419)

**What changed**: "Like every default fold, an unknown/unfoldable default never rejects" becomes a
statement that a default that cannot be folded rejects as well, because it leaves the base case of the
induction open, with one designed exception — a default the construction event provably overwrites — and a
note that the current implementation emits only on a proven-`false` fold and that this is a known gap.
**Warrant**: category 1. want:18 — "**Base case.** The default configuration satisfies every rule, and
every initial event *establishes* every rule, provable from the constraints declared on its args" — with
want:171, "A business rule whose enforcement the compiler cannot prove complete is rejected." want:124
supplies the proven-violating half in the must-not-compile table: "Change the default to `20000.0` |
Default value violates `max 10000.0` — the base case has a counterexample."

### D9 — the worked emission example's severity (lines 520–521)

**What changed**: the example output for `UnreachableState` changes from `Diagnostic(Warning, …)` to
`Diagnostic(Error, …)`, with a comment saying this is the ruled severity and that `Diagnostics.cs` still
carries Warning.
**Warrant**: category 1. want:151 — "**Every state is reachable.** … A state no path reaches is rejected" —
and want:125, the must-not-compile row "Add `state Suspended` with no inbound row | Unreachable state",
under a table whose preamble says each row "**must be a compile-time rejection**". Note this is not walk
item 22, which covers three other codes.

### D10 — who reads `ProofRequirementMeta.DiagnosticCode` (line 540)

**What changed**: "the same field `CreateFaultSiteLink` reads" becomes "`CreateDiagnostic` reads this …
and it is the only reader".
**Warrant**: category 1, inherited from D11/D12 — `CreateFaultSiteLink` belongs to the removed model
(want:165), and leaving it as the evidence for a live claim would have grounded the argument on a function
that no longer exists.

### D11 — the `[StaticallyPreventable]`-derived fault map (line 551)

**What changed**: the "conservative backstop for proof-only obligation families that have no representable
runtime fault of their own" is deleted, leaving one piece of explicit policy rather than two. In the same
sentence, "the attribute is the single source of truth" is reworded to "the attribute is where that map is
declared and the only place it is declared."
**Warrant**: category 1 for the deletion — want:171, "In an ideal world there is **no deferral**. A
business rule whose enforcement the compiler cannot prove complete is rejected — never handed to the
runtime on the compiler's own initiative"; a backstop for obligations the proof engine could not discharge
is that hand-off. Category 3 for the rewording: `CLAUDE.md` § How to write bans the noun phrase, and the
sentence was being rewritten anyway.

### D12 — the chain's opening, its diagram, and what runs when (lines 557, 562, 565, 569)

**What changed**: "The evaluator must never produce a failure that the compiler could have prevented"
becomes "Every class of failure the language admits is one the compiler is responsible for preventing";
the diagram loses its first link, "Evaluator fails →"; a sentence is added saying the whole chain is
checked when the project is built and none of it runs while an event is being evaluated; and a note is
added above the enum excerpt saying `fault-system.md` declares the enum and is the only place its
membership is listed.
**Warrant**: category 1 — want:165 for the first three. The note about not keeping a second copy is
category 3 and shares D13's warrant.

### D13 and D14 — the duplicated `FaultCode` enum and `Faults.GetMeta` switch, reduced to excerpts (lines 586, 602)

**What changed**: a full thirteen-member copy of the enum and a matching thirteen-case copy of the switch
are replaced by two members each plus a pointer at `docs/runtime/fault-system.md`.
**Warrant**: category 3, internal repair, and the two statements that disagreed are worth naming: this
document's copy stopped at `OutOfRange = 13` and omitted `LengthBoundViolation` and `CountBoundViolation`,
which `fault-system.md` declares, and it paired `UnexpectedNull` with `NullInNonNullableContext` where
`fault-system.md:125` says `UnprovedPresenceRequirement` and `soundness-and-coverage.md:226` independently
agrees. `CLAUDE.md` § Catalog System also forbids maintaining a parallel copy of what a catalog knows.
The teaching content — what the attribute looks like on a member — is kept.

### D15 — the § Evaluator usage section deleted (was lines 645–657)

**What changed**: the section is gone. Its entire content was:

```csharp
// Every failure path uses the enum — no bare string errors:
case BinaryOperator.Divide:
    if (IsZero(right))
        return Fail(FaultCode.DivisionByZero);
    return Success(left / right);
```

**Warrant**: category 1, and it is the sharpest in the file. want:165 names this exact check when it says
what a lighter runtime means: "No divide-by-zero tests, no overflow guards, no re-checking of proven bounds
on writes — the evaluator just computes." want:104 says the same about the worked example: "At runtime
there is no zero-check anywhere — the ingress validation of the arg is what makes the division safe."

### D16 — the enforcement-chain walkthrough uses a generic member (lines 631, 634, 636)

**What changed**: the walkthrough that taught the chain by adding `FaultCode.StackUnderflow` now adds a
generic member `X`, described as "a class of failure some pipeline stage can diagnose against the author's
source text", and the first step (`PRECEPT0001: Fail() must use FaultCode`) is dropped.
**Warrant**: category 1 for dropping the step — `PRECEPT0001` constrains `Fail()` call sites, and want:165
removes them. Category 3 for the generic member: `StackUnderflow` is a defect in the engine, which is
exactly the case six canonical places now record as undecided, so teaching the chain with it as the example
would have taught something canon does not settle.

### D17 — new paragraph: what the chain rules out (line 648)

**What changed**: a paragraph is added saying a `FaultCode` member is admissible only when a real authoring
diagnostic exists for it; that a defect in the engine has none, because there is no source text an author
could change in response and nothing to put in the message template; and that whether the enum is therefore
closed, or `PRECEPT0002` relaxed for an engine-defect category, is undecided — pointing at
`fault-system.md` Q3.
**Warrant**: category 3, and it is one of the two changes here that goes beyond repairing a contradiction —
the inference that the category "cannot be added to `FaultCode` as the rules stand" is the editor's, drawn
from the chain's own stated rules, not from anything he wrote. The repair part is real: after D16 removed
`StackUnderflow` from the walkthrough, nothing said why that example had gone. Recording the question as
undecided is what the 2026-08-11 deferral requires. Quoted in full at the end.

### D18 — the divide-by-zero example, re-authored from the compiler side (lines 665–684)

**What changed**: step 1 was "Evaluator: `a / b` where `b == 0` → `Fail(FaultCode.DivisionByZero)`"; the
five steps are now four, opening "Take a definition containing `a / b`, where the compiler cannot show `b`
is non-zero", and closing with a new line: "The chain ends at a definition that does not compile. A
definition that does compile carries no divisor test, because the proof is what makes the test
unnecessary."
**Warrant**: category 1 — want:165 and want:104, as for D15. The re-authoring rather than a straight
deletion is because removing step 1 alone would have left the walkthrough starting nowhere.

### D19 and D20 — "every evaluator failure mode" (lines 721, 741)

**What changed**: both sentences now say "every failure class the language admits" instead of "every way
the evaluator can fail at runtime" / "every evaluator failure mode".
**Warrant**: category 1, inherited from D12 — want:165.

---

## `docs/compiler/soundness-and-coverage.md`

### S1 — the document's own summary line (line 16)

**What changed**: "the deferred independent re-checker" becomes "the independent checker".
**Warrant**: category 1, inherited. Mechanical consequence of S3 and S5.

### S2 — new note defining what "the MVP" means (line 28)

**What changed**: a note is added at the head of the document saying the MVP is the first working compiler,
an internal build milestone, not a release, and that "post-MVP" places work later in the build order rather
than describing a gap the product carries.
**Warrant**: category 3, internal repair, and the second of the two changes that adds more than the repair
needed. The contradiction was real: four sites in the specification said the overflow model was deferred
"past the first release", while `soundness-and-coverage.md:226` in the same tree said "in-scope,
not-yet-implemented", and his own ruling at walk item 15 defers it "past the MVP", not past a release. Two
independent readers took the "first release" text at face value and concluded the product ships a known
gap. What the note adds beyond that repair is a definition of the term, and the sentence "nobody outside
the project receives it" is not sourced to any document in the reading list — the nearest record of his
position is a conversation of 2026-05-26 captured in project memory ("this thing is not going anywhere for
a while… Nothing ships to external users"). Quoted in full at the end.

### S3 — how soundness is delivered (lines 55–62)

**What changed**: "because the MVP ships no live re-checker (see §5b), MVP soundness comes from fixing the
known fail-open holes in the engine directly" is replaced by a bullet describing an independent checker
with two call sites — a compiler stage and a load-time gate in the runtime — and the direct-engine bullet
is kept but re-framed as complementary, with the honest limit that a replay cannot see an obligation the
engine never created.
**Warrant**: category 1. want:201–204 — "One small checker component, two call sites: **A compiler stage** —
audits the proof engine's work at authoring time, catching proof-engine bugs when they are cheapest to fix.
**A load-time gate in the runtime** — the same checker, run once when a definition is loaded. The runtime
refuses to govern under a definition whose certificate does not verify." Category 2 support: the plan's
§ 3 ruled row, "**The certificate checker is in scope**, including its runtime load-time half", which he
marked *mine* at walk item 1 with the words "mostly mine", the walk recording that this row "is want:201–204
almost verbatim".

### S4 — why there is no band-versus-rule boundary, and the removed parenthetical (lines 98–113)

**What changed**: the heading changes from "Why the boundary is proof-carrying-vs-not, never band-vs-rule"
to "Why there is no band-vs-rule boundary", and the deciding question changes from a per-operand structural
test to whether the four premise classes close the proof. Separately, the parenthetical "(The
`[StaticallyPreventable]` evaluator traps are defense-in-depth for out-of-contract entry, not an
author-facing opt-out.)" is deleted.
**Warrant**: category 1 on both. The heading and the reasoning rest on want:22 (one constraint mechanism)
and want:18–19 (base case, inductive step, four premise classes). The parenthetical goes on want:165, which
removes the traps, and on want:141, which removes the case it was defending against: "Restore/rehydration
is *not* ingress — data coming back from persistence is trusted and unchecked, because it was governed when
it was written." The point the parenthetical was making, that there is no author-facing escape hatch,
survives in the sentences above it.

### S5 — § 5b rewritten: the checker is in scope, and is not built (lines 145, 152–173)

**What changed**: "### 5b. Independent re-checker (deferred) … **Deferred — not in the MVP**" becomes
"### 5b. The independent checker — in scope, both call sites", with both call sites described, followed by
a plainly-labelled **Not built**: no certificate is emitted, no checker exists, and a `Proven` verdict is
minted by the discharge cascade rather than by passing a replay. In § 5a, "the format the MVP ships"
becomes "the format the MVP produces".
**Warrant**: category 1 for the scope change — want:201–206, including "Every step is re-verified; the
figuring-out is never repeated. That's why checking stays fast and the checker stays small", which the new
text paraphrases closely, and want:204's requirement that the runtime "refuses to govern under a definition
whose certificate does not verify". Category 2 support as in S3. The "ships"→"produces" wording is
category 3, inherited from S2.

### S6 — the `QualifierMismatch` coverage row (line 231)

**What changed**: the row loses "*(runtime fault-identity collapse to a single backstop is a separate
runtime-only concern — see §3.2)*" and reads simply **in**.
**Warrant**: category 1, inherited — want:165. With no fault produced at evaluation time there is no
runtime fault identity to collapse.

### S7 — the residue note reduced from two items to one (line 265)

**What changed**: "Runtime-only residue (not a compile-time coverage gap)" listed two items; the second,
the `FaultSiteLink` identity collapse, is deleted with `FaultSiteLink`, and the note is retitled "A
message-template defect, not a compile-time coverage gap". Item 1 — assignment-out-of-declared-bounds
borrowing the `NumericOverflow` wording — survives untouched.
**Warrant**: category 1 for the deletion (want:165); the retitle is a mechanical consequence.

### S8 — § 6's opening (line 318)

**What changed**: "With the independent re-checker deferred (§5b), MVP soundness comes from fixing the
engine directly" becomes "Until the checker of §5b is built, nothing replays a derivation, so soundness
rests entirely on the engine being right — which is why the fail-open holes are fixed directly".
**Warrant**: category 1, inherited from S5 — want:201–206.

---

## Traces to nothing he wrote or ruled

> **Dispositioned 2026-08-26 — both of the two changes quoted below are closed.** The note defining "the
> MVP" was put to him directly and **he confirmed it**: *"yes"*. `soundness-and-coverage.md` now records
> that, dated, in the note itself. "What the chain rules out" **needs no action** — it is an inference he
> never wrote, but it is drawn from rules the document already states, and it marks the unresolved part as
> unresolved and points at the parked question. His words and his reasons:
> [`../trace-walk-rulings.md`](../trace-walk-rulings.md).

**Nothing in these two files falls entirely into this category.** Every deletion of the fault-checking
model traces to want:165 or want:171; every reclassification of a bound traces to want:22; the checker's
return to scope traces to want:201–206; the author-visible verdicts trace to his 2026-07-26 ruling.

Two changes add canonical text that goes further than the contradiction it was fixing. Neither contradicts
anything of his, and both are honestly marked in the documents themselves, but the extra content is the
editor's judgement rather than his position, so both are quoted here in full.

### 1. `diagnostic-system.md:648` — "What the chain rules out"

**Before**: nothing. This paragraph did not exist; the section ended at "Every step is compiler-enforced or
Roslyn-enforced. One custom attribute. Two exhaustive switches. 24 Roslyn rules (PRECEPT0001–PRECEPT0024).
The chain is unbreakable without making the build fail."

**After** (added in full):

> **What the chain rules out.** It runs from a failure class to the diagnostic that prevents it, so a
> member is admissible only when such a diagnostic genuinely exists — one a pipeline stage emits against
> source text, carrying a stage, a severity and a message template. A defect in the engine itself — a stack
> underflow in the opcode loop, say — has no such diagnostic: there is no source text an author could
> change in response to it, so the diagnostic the chain demands would have nothing to put in its message
> template. That category cannot be added to `FaultCode` as the rules stand. Whether the enum is therefore
> closed at the members it has, or PRECEPT0002 is relaxed for an engine-defect category, is undecided — see
> `fault-system.md` Q3.

**What was searched for and not found**: the want document says nothing about which members `FaultCode` may
carry, and no ruling in the walk or the plan's § 3 or § 4 addresses the admissibility of an engine-defect
member. The plan's § 4 row of 2026-08-11 does cover the neighbouring question — whether anything ever
constructs a `Fault` — and defers it in his words ("do we need to decide this now? seems like something we
can defer until we start building the runtime"), which is why the last sentence, recording the question as
undecided, is warranted. The sentence that goes beyond that is "That category cannot be added to
`FaultCode` as the rules stand", which is an inference from the chain's own mechanism.

### 2. `soundness-and-coverage.md:27–33` — the note defining "the MVP"

**Before**: nothing. The document went straight from the note about why Part A and Part B share one file to
the `---` before Part A.

**After** (added in full):

> **What "the MVP" means in this document, and everywhere else in `docs/`.** The MVP is the first working
> compiler — an internal build milestone marking when the prove-or-reject engine runs end to end. It is not
> a release, and nobody outside the project receives it. So "post-MVP" and "after the MVP" place work later
> in the build order; they never describe a gap the product is meant to carry, and nothing in these docs
> should be read as saying a deferred item is missing from something a user gets.

**What was searched for and not found**: the want document uses "MVP" twice (want:191, want:195) and never
defines it. The walk's item 15 defers overflow "past the MVP" — that word is his ruling's word, and
restoring it in place of "past the first release" is the repair. What is not in the want document, the
walk, or the plan is the definition itself: that the milestone is internal and that nobody outside the
project receives it. The nearest record of his saying so is a conversation of 2026-05-26 kept in project
memory — "this thing is not going anywhere for a while", "Nothing ships to external users" — which is a
recorded quotation but not one of the documents this trace takes as authority. If he disagrees with the
definition, this note is where it is written down, and `precept-language-spec.md:272` now repeats it
inline ("the MVP, the first working compiler, which is an internal milestone and not something anybody
receives") and points back here; five further sites in the specification and `primitive-types.md` were
reworded away from "first release" in the same pass.

---

## Observations — recorded, not changed

Three things found while reading that are not changes made in this range.

1. **A pre-existing paragraph now sits in tension with the item-11 ruling.** At
   `diagnostic-system.md:162`, the `CountBoundViolation` description says an unprovable count change — "a
   provable violation **or** a merely-unprovable case" — emits the same guard-naming message. The new
   paragraph at `:183` requires the author to be able to tell those two apart. The diff changed only the
   opening sentence of `:162`, so this is not a change made here, but the two paragraphs of the same
   document now pull in different directions, and the message shapes belong to phase 4.

2. **The `PRE0078` message defect named in the ruling's own preparation is untouched here.** The
   what-a-refusal-claims paper reproduces "Numeric computation exceeded the representable range" on a
   provable program and calls it the failure the ruling forbids. D4 corrects the *classification*
   (`NumericOverflow` names a value leaving a declared bound, not machine overflow), which is a step toward
   it, but the message template itself is not in these two documents.

3. **The severity model states a target the code does not implement, and says so.** Lines 189–193 now list
   seven codes whose `Diagnostics.cs` severity is tracked drift, `UnsatisfiableGuard` newly among them.
   That is the arrangement walk item 23 endorses — the document states the target and the code is validated
   against it — but it means these lines cannot be checked against the compiler, only against the want
   document, which is how they were checked here.
