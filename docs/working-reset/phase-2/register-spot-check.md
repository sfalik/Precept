# Spot-check of the phase-2 contradiction register

**Status**: Review — 2026-07-28
**Subject**: `contradiction-register.md`, 141 rows, sampled at fifteen
**Not checked**: whether the grep patterns return zero. Somebody else did that, and it establishes
almost nothing. This check is about substance.

## How the sample was drawn

Stated before the rows were read, so a reader can see it was not selective.

**Eight executed rows.** Executed rows were grouped by the document each one corrects — the
specification, the runtime documents, the architecture document, the compiler documents, the type
documents. From the three largest groups (specification, runtime, compiler) I took the **first and the
last** executed row in register order. From the other two I took the **first**. That gives:

| Group | First | Last |
|---|---|---|
| `precept-language-spec.md` | `S1` | `S15` |
| `docs/runtime/` | `R1` | `N69` |
| `docs/compiler-and-runtime-design.md` | `N11` | — |
| `docs/compiler/` | `N97` | `C3` |
| `docs/language/` type documents | `N31` | — |

**Four of the nineteen deferred fault-backstop rows.** The nineteen are `R9`, `N22`, `N23`, `R10`,
`N24`, `N25`, `R14`, `N26`, `R11`, `R12`, `N27`, `R13`, `N28`, `N29`, `C1`, `C2`, `N30`, `N34`, `N35`,
in register order. Positions 1, 7, 13 and 19: **`R9`, `R14`, `N28`, `N35`**.

**Two rows recorded as not executed for a stated reason.** The rows outside § 3 and § 13 whose commit
cell says "Not executed", in register order, are `N8`, `N10`, `N66`, `N70`, `N100`. First and last:
**`N8`, `N100`**.

**The editable-write premise**: `N96`.

Each row was checked five ways — the canonical text against `git show <commit>^:<path>`, the want line
against `phase-1/want-document-provenance.md` before it was used, the corrected text as it now stands,
whether the resolution follows from the evidence, and whether the walk records the ruling the row leans
on.

---

## The rows

### S1 — the post-mutation sweep paragraph in spec §3A.4 — **arguable**

The quoted paragraph is verbatim at `5e853c0e^:1969`, including "Ingress governs *what enters*; the
sweep governs *the result*." It is gone at HEAD; §3A.4 is now atomicity mechanics plus an ingress
sentence scoped to "the declared contract of the slot the value enters and nothing wider". want:199 is
Shane's and says exactly what the row says it says.

What makes it arguable is the reason cell. It justifies the narrow ingress scope by citing "walk items
7, 8, 16". Walk items 7 and 8 are about `ensure` obligations and activation-condition write sites, not
about ingress scope. Walk item 16's recorded answer says the opposite of what the row draws from it:
*"The one licensed piece underneath the canon claim: evaluating every rule mentioning an editable field
at the editable write is a premise you name (want:143)."* The specification itself now carries a note
saying this — "the narrowing was written on 2026-07-27 on a reading of the owner's walk item 16, and
that item's recorded answer preserves want:143 rather than replacing it" (`:276`) — but the register row
was never corrected to match. The deletion of the sweep is right; the stated authority for what replaced
it is not. Mitigated: the text is marked disputed in two places and the question is routed to `N96`.

### S15 — "an unknown/unfoldable default never rejects" — **sound**

Verbatim at `5e853c0e^:213`. At HEAD it reads "A provably-false fold rejects. So does an unknown or
unfoldable one", with the base-case reason and one designed exception for a default the construction
event provably overwrites. want:18 ("The default configuration satisfies every rule") and want:124 are
both Shane's. The flip follows from want:18 plus want:189's refusal of "checks silently deferred". The
placeholder exception is an inference rather than a quoted line, but it narrows canon toward the want
rather than away from it.

### R1 — `ConstraintsFailed` in the `Create` outcome table — **sound**

Verbatim at `c3a9be21^:171`. At HEAD the outcome has moved to the structurally-impossible list with the
reason attached. Straight consequence of want:199.

### N69 — the premise list as the runtime's validation set — **sound**

An ADDITION row. `runtime-api.md:258` now says the premise list "*is* the set of runtime validations the
proofs depend on … derived from the proof rather than maintained as a parallel list alongside it."
want:210 is Shane's and this is close to verbatim.

### N11 — the architecture document's working-copy-discard clause — **sound**

Verbatim at `25894ae3^:133`. Gone at HEAD; the ingress half survives and the surrounding passage now
states that a declared premise, not provenance, decides prove-versus-govern. Consistent with want:199
and with walk item 16.

### N97 — the constraint re-check index in `proof-engine.md` — **sound**

Verbatim at `3dbad40b:2237`, including "Used by the evaluator for constraint re-check". At HEAD `:2243`
states flatly that it is not a re-check index and says why; `:2255` corrects the builder table. The
remaining question (whether ingress routing is served by this map or the bucket index) is held with
`N80` rather than answered. Right disposition.

### C3 — the count-interval seed grounded on a runtime sweep — **sound**

Verbatim at `d32d8e26^:1733`: "(the field already satisfies its band entering the chain — §3A.4 swept
prior state)". At HEAD `:1739` re-grounds it on "every rule holding in the pre-state is a premise the
induction supplies". That is want:19 premise class (d), Shane's line. This is the register's sharpest
row and it was executed exactly as written.

### N31 — "a runtime trap is defensive redundancy" in `primitive-types.md` — **sound**

Verbatim at `4ca55318^:658`. At HEAD the trap clause is gone, prove-or-reject survives, and the
non-finite table now disposes `±∞` as deferred with the overflow model — which is walk item 15, ruled
*mine*, applied correctly.

### R9, R14, N28 — three of the deferred fault-backstop rows — **sound**

All three quote canon accurately and all three are genuinely unexecuted:

- `R9` — `precept-builder.md:426` and `:607` still carry the `FaultSiteAnnotation` planting contract and
  "Fault backstops are defense-in-depth" word for word.
- `R14` — the "Impossible-Path Failures (Defense-in-Depth)" section stands at `evaluator.md:1624` with
  the division-by-zero row at `:1630` (the register's `:1654` is a stale line number).
- `N28` — `runtime-api.md:678` still says "15 `[StaticallyPreventable]` fault codes exist at evaluation
  time as a backstop" (register cites `:660`, stale).

Each resolution follows directly from want:165, which is verbatim Shane. Deferring them is a legitimate
scope call. See the note below on the *reason* recorded for the deferral, which is a separate problem.

### N35 — the diagnostic system's "conservative backstop" — **arguable, leaning wrong**

The text is accurate and unexecuted, at `diagnostic-system.md:552`. But read in place, it is not a
runtime fault trap. It sits in § *The `[StaticallyPreventable]`-derived fault map* and describes two
pieces of **compile-time** policy the proof engine keeps as code because the attribute cannot express
them: the collection-safety many-to-one collapse, and which diagnostic code to attribute when an
obligation family has no runtime fault counterpart. want:165 removes checks at *evaluation* time. The
register's own § 1.0 triage makes exactly this distinction for `descriptor-types.md:113` and `:178` and
rules that text legitimate — "compile-time attribution, not a runtime pass". `N35` reads the same shape
the other way. Its reason cell is the single word "**New.**", so there is no argument to weigh against
this. If the row executes as written, a compile-time diagnostic-attribution rule is deleted on the
strength of a sentence about runtime backstops.

### N8 — OQ-4 and the event ensures — **arguable**

The quoted text is verbatim at `c3a9be21^:evaluator.md:940` and `result-types.md:209`, and the
resolution is right: OQ-4's two options both assume a post-mutation working copy that no longer exists.
But the commit cell says "Not executed", and says the runtime slice left "the OQ-4 note this row cites"
in place. It did not. `c3a9be21` rewrote both sites — `evaluator.md:912` now says the empty array "is
part of the open question below rather than a wiring task", and `result-types.md:215` says OQ-4 "used to
ask whether it should move inside `TransitionInspection` … that option describes a pipeline the language
no longer has." The first half of `N8`'s resolution was executed. Only the second half (what inspect
reports instead) is genuinely open, and that half is correctly held behind `N79`. The row understates
what was done, which is the opposite of the usual failure but still a wrong cell.

### N100 — the strategy numbering — **sound**

Verified directly. The `ProofStrategy` enum at `:304`–`:316` declares `CompositionalConstraint = 6`,
`IntervalContainment = 7`, `LengthContainment = 8`, `CountContainment = 9`, `DimensionalProduct = 10`.
The headings place Dimensional Product at `:1474`, Compositional Constraint at `:1490`, and Interval,
Length and Count at `:1705`, `:1715`, `:1733`. The prose follows the headings (`:505`, `:601`, `:630`).
Every strategy from 6 up does carry two numbers. Routing it to the owner is right — the fix changes
either a code-facing enum's values or ten headings, and the disclosure at `:155` puts the disagreement
where a reader meets the count.

### N96 — the editable-write premise — **sound**

The row's account is accurate at HEAD. Canon says the narrow thing consistently and says it in two
places, both marked disputed (`precept-language-spec.md:276` and `:1994`). want:143 says the wide thing
and is safe to quote — it is `ed57d7fd`'s reword of `72c2e183:131`, and provenance records it as a
vocabulary change with the claim intact. The consequence the row names — a field declared `editable` and
mentioned in a relational rule cannot be proven from premise classes (a) and (d) alone — is real. Routing
it to the owner rather than settling it is right.

One caveat, and it does not change the verdict. The row cites `want:36` and `want:40` as the want
document's own worked example of the shape. Both lines are `73f4db7d`, which `want-document-provenance.md`
declares unsafe to quote. The register row does not say so; the phase-5 files it points at do, both of
them, in terms. Nothing rests on it that is not carried independently by want:143.

---

## Two things that cut across the sample

### The fault-backstop family was split, and the recorded reason for the split is not accurate

All nineteen deferred rows carry the same sentence: the family is "one coherent group across
`evaluator.md`, `precept-builder.md`, `fault-system.md`, `descriptor-types.md`, `runtime-api.md` and the
compiler-side `C1`-`C4` rows, of which only two files were in any single slice's scope."

Two problems.

**`C4` is in that list and `C4` was executed**, by `d32d8e26`. Its replacement text at
`proof-engine.md:1737` now reads "the runtime evaluates no constraint against the finished working copy
and carries no fault trap at evaluation time". Meanwhile `C1` and `C2` in the same document are
untouched: `:123` still lists FaultSiteLink production for "Precept Builder backstops" and `:2457` still
says backstops "exist for belt-and-suspenders safety". One file, one commit, two contradictory
statements. `runtime-api.md:678` still promises the fifteen evaluation-time fault codes that
`proof-engine.md:1737` says do not exist. **Phase 2 created this contradiction**; it was not there before.

**The file-scope justification does not hold.** Of the six files, five were edited by phase-2 commits,
several more than once. `fa4891d7`, the fourth pass, touched `evaluator.md`, `runtime-api.md`,
`proof-engine.md` and `soundness-and-coverage.md` in a single commit. Only `fault-system.md` and
`descriptor-types.md` were never opened. The deferral may still be the right call — nineteen rows that
have to move together is a real reason to hold them — but the reason recorded is not the reason, and a
reader deciding whether to trust the deferral is reading a claim that the files were out of reach when
four of them were being edited together.

### A row outside the sample, found while checking provenance

Not sampled, so it is reported separately rather than counted.

`N59` is an ADDITION row that wrote a new normative paragraph into `precept-language-spec.md` §0.7 — the
non-linear rule, the rejection disposition, and the message shape. It cites `want:135`, `want:105` and
`want:123`. **All three are `73f4db7d` lines.** `want-document-provenance.md:103` names `:105`, `:123`
and `:135` explicitly among the eleven lines it says are "not safe to quote", and says a verdict resting
on any of them "is resting on text an agent wrote inside the document that everything else in this phase
measures against."

The executing document goes further than the register does.
`recheck-gap-corrections-executed.md:488` says of the new paragraph: "**Every clause is the want
document's.** … Nothing here is a new design decision; it is the owner's own statement moved from the
want document into the specification." It is not his statement. Nothing in the register or in the
correction record flags the provenance.

Whether the paragraph is *right* is a separate question and it may well be — it agrees with
prove-or-reject, and walk item 16 points the same way. But it is now normative canon with a rejection
rule and a message shape in it, presented as the owner's own words, and he never wrote it. `N56` also
cites want:135. The rows that cite `want:36`, `:40`, `:71`, `:72`, `:80`, `:81`, `:105`, `:121`, `:122`,
`:123` or `:135` are the ones to check next.

---

## Verdict on the sample

| Verdict | Count | Rows |
|---|---|---|
| Sound | 12 | S15, R1, N69, N11, N97, C3, N31, R9, R14, N28, N100, N96 |
| Arguable | 3 | S1, N35, N8 |
| Wrong | 0 | — |

**On this sample the register is trustworthy for what it claims about canon.** Every canonical quotation
checked was verbatim against the pre-correction tree — fifteen for fifteen, including three where the
recorded line number had drifted but the text was there. Every executed correction did what its row said
it did. No row invented a canonical sentence, and no row claimed an edit that had not happened. That is
the failure mode this check was most likely to find and it did not find it once.

The three arguable rows are all the same kind of thing, and none of them is a bad edit. `S1` records an
owner ruling as authority for something the ruling does not say — the edit is right, the citation is not,
and the specification already carries a note admitting it while the register does not. `N35` reads
compile-time text as runtime text, against the register's own triage of an identical case. `N8` says
"not executed" about work that was executed. Two are bookkeeping; one, `N35`, would delete the wrong
sentence if executed as written.

**What fifteen rows do and do not establish.** Three arguable in fifteen is a rate of one in five. On 141
rows that projects to something like twenty-eight rows worth a second look — but the projection is worth
very little, because the sample was drawn to be representative of *documents*, not of *risk*, and the
risk is not evenly spread. The register's own history says so: seven rows carried a "NOT ACCOUNTED FOR"
label that turned out to be wrong, six patterns were written from memory of a sentence rather than copied
from it, and five rows carried the wrong kind of pattern for two passes. Those clustered. What fifteen
rows does establish is a floor: the quotation discipline holds, the corrections landed, and the errors
found are in the reasoning cells and the commit cells rather than in the canonical text.

**The one thing that would change the answer** is the provenance question. It is not visible from a
sample stratified by document, because it follows the want-document line numbers instead. One row outside
the sample (`N59`) put a normative paragraph into the language specification on the strength of three
lines an agent wrote, and the correction record calls them the owner's own words. That is a single row,
and it might be the only one. Checking the rows that cite the eleven unsafe lines is cheap and would
settle it.
