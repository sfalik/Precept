# Phase 2 — independent re-check of conditions 3 and 4

**Run 2026-07-27 at HEAD `d32d8e26`** by someone who did not write the search procedure, the register,
or any of the four correction slices. Nothing in the canonical tree was edited and nothing was
committed. `docs/Working/bugs.md` and `docs/working-reset/phase-5/` were not opened.

**What was cited**: the canonical documents at HEAD, `docs/Working/what-i-want-2026-07-16.md`, the
phase-1 register and walk, and the phase-2 working documents. No catalog, no sample, no compile.

## Verdict

| Condition | Verdict |
|---|---|
| 3 — every executed row's recorded pattern returns zero | **Fails.** Four rows the execution records claim executed still return hits on the pattern the register recorded for them, and one of those four had a different, narrower pattern substituted in the execution record without the register being amended. |
| 4 — a second person re-running the procedure finds no disagreement that is not already a row | **Fails.** Six disagreements found that no row covers. Two of them were *created* tonight: a document corrected in one section now contradicts itself in another. |

Neither failure is a wrong resolution. Every correction I read is, on its merits, the right change in
the right direction. What failed is coverage and bookkeeping — and one of the coverage holes is in the
architecture document the project sends every new reader to.

---

## 1. The procedure, run as written

It runs. § 0's two shell variables, § 3's sixteen greps, § 4's three absence greps and § 7's Python
resolver all execute without editing. The resolver reports **0 problems** — every `file.md:NNN` cite in
the register still resolves and is in range at HEAD.

Three things I had to interpret, and a re-runner would interpret differently:

1. **§ 3 says "read the paragraph around it and place it in one of four buckets" and gives no rule for
   the boundary between bucket 3 (vocabulary reuse, discard) and bucket 1 (contradiction, a row).** A1
   returns 34 hits at HEAD, of which I judged 4 to be disagreements and the rest reuse. Somebody else
   would draw that line in a different place. This is the procedure's real cost, and § 6 blind spot 1
   understates it: the weakness is not only that a grep is keyed to a word, it is that the *hit
   triage* is unrecorded. A second run cannot reproduce the first run's discards because the first run
   did not write them down.
2. **§ 1's excluded-paths table names `docs/README.md`, `docs/agent-onboarding.md` and `docs/glossary.md`
   but is silent on the four per-area READMEs** — `docs/runtime/README.md`, `docs/compiler/README.md`,
   `docs/language/README.md`, `docs/tooling/README.md`. Those sit *inside* the canonical folders, so
   `$CANON` includes them and the greps hit them. I treated them as in scope. One of them carries a
   disagreement (§ 4.4 below).
3. **§ 5a says "re-verify every line again at the current HEAD"** but the correction list's line numbers
   were verified against `3f69b316`, and four commits have moved the canonical documents since. I
   re-verified against HEAD and the register's own numbers hold, but only because the register was
   rebuilt at `b93d1db1`; the instruction as written points a re-runner at a stale baseline.

**One thing in the procedure is better than the plan's own condition.** § 1's widening to
`docs/compiler-and-runtime-design.md` was the right call and it earned its keep: fifteen register rows
and the single densest concentration of removed-model text were in a file the plan's finish-line grep
does not cover. Nobody should narrow that back.

---

## 2. Every recorded grep pattern, run at HEAD

All 111 patterns in the register that carry a runnable command were run against `$CANON` or
`$CANON_WIDE` as the row directs. Rows whose own text says the pattern is expected to return more than
zero (`N3`, `N10`, `N29`, and the nine `ADDITION` rows) were scored against that expectation instead.

### 2.1 Rows the execution records claim executed, whose pattern does not return zero

| Row | Pattern | Hits | What is actually there |
|---|---|---|---|
| **S7** | `ConstraintsFailed` | 15 | `UpdateOutcome.ConstraintsFailed` survives deliberately, re-scoped to an ingress failure (`result-types.md:139`, `:148`, `:152`; `runtime-api.md:395`, `:475`; `evaluator.md:359`, `:743`, `:1610`), and four sites now say the outcome is structurally impossible. That is a defensible resolution — but the register's recorded pattern can never return zero for it, and `spec-corrections-executed.md` quietly substituted the narrower `Post-mutation constraint violations` in its own grep column. The register was not amended. |
| **N15** | `EventConstraintsFailed\|UpdateConstraintsFailed\|RestoreConstraintsFailed` | 2 | `runtime-api.md:273`, `:282` — see N19 below. The architecture document's copies are gone; the runtime document's are not. |
| **N20** | `RestoreConstraintsFailed` (WIDE) | 2 | Same two sites. `architecture-doc-corrections-executed.md` marks N20 executed because it cleared the architecture table's cell; the pattern is repository-wide and the runtime copy was never touched. |
| **N36** | `defense-in-depth` (WIDE) | 13 | Zero in `compiler-and-runtime-design.md` — the seventeen sites really are gone, and that edit is the strongest single piece of work in the phase. The thirteen remaining hits are the deferred fault-backstop family in five other files. |

For N15, N20 and N36 the row is not wrong and the correction is not wrong: the *pattern* is
repository-wide while the *row* is file-scoped, so the row cannot report itself done until an unrelated
slice finishes. That is a scoping mismatch, not dishonesty. **S7 is different** — there the executed
record used a pattern the register does not contain, and the register still shows an empty commit
column, so nothing in the written record connects the two.

### 2.2 Patterns I judge too narrow to prove what they claim

**`SC-1` and `N65` — the carried-constraint model.** This is the one I would call dishonestly narrow,
and it is the one that let a real leftover through.

- `SC-1` greps for `carries a constraint the engine can discharge`.
- `N65` greps for `carries a sufficient constraint`.

Both now return zero, and both are exact single clauses. The superseded idea — *the compiler proves
that an operand carries a discharging constraint, rather than proving the rule by induction from four
declared premises* — appears in at least four shorter forms, and three of them are still in the tree:

- `compiler-and-runtime-design.md:149` — "The proof engine discharges each obligation by establishing
  that the operand **carries a discharging fact**"
- `compiler-and-runtime-design.md:153` — "The compiler never inspects the value — it proves the
  **structural 'carries a constraint' fact**"
- `compiler-and-runtime-design.md:169` — the composition seam restating the same

A pattern that greps a whole clause when the idea has three shorter spellings is a pattern that passes
while the wording stays in place. That is exactly the failure mode the plan warns about, and here it
produced a live contradiction — see § 4.3.

**`N55` — the base-case assumption.** Pattern `assumed for restored`, required to return more than
zero, returns zero. The text *was* written, at `precept-language-spec.md:278`, in the words "*proven*
for data that arrived through the contract and *assumed* for data that did not." The pattern tests a
phrase nobody wrote. Same shape at `N52` (`proof by induction` returns zero; the induction is at
`:268` and `proof-engine.md:2696`) and `N53` (`premise class` returns zero; the closed set is named at
`:268` as "four classes of premise"). Three ADDITION rows would be scored as failures by their own
recorded test while the work behind them is done and done well. The inverted patterns were written
before the text existed and were never re-aimed at it.

### 2.3 Rows never executed, never disclosed, with the superseded wording still live

These are register rows whose pattern returns hits, which appear in **none** of the four execution
records — not in a rows-executed table, not in a "deliberately not touched" list, not in a
"contradictions found outside the register" note. They were not deferred; they were dropped.

| Row | Site still live at HEAD |
|---|---|
| N8 | `result-types.md:213` — the OQ-4 open question, still asking what to populate "against the post-mutation working copy" |
| N9 | `result-types.md:375` — the OPA comparator row's "Collect-all violation set" |
| N19 | `runtime-api.md:273`, `:282` — `RestoreConstraintsFailed` in the caller example, plus canon's own note deferring its removal "when the Restore API is finalized". The register's resolution was explicit: *"Phase 2 owns it now."* |
| N31 | `primitive-types.md:658` — "a runtime trap is defensive redundancy, never the primary mechanism" |
| N32 | `primitive-types.md:654`, `:655` — "Runtime fault — integer overflow" |
| N33 | `collection-types.md:766` — "Runtime fault checks exist only as defensive redundancy for paths the compiler has already proven unreachable" |
| N40 | `collection-types.md:627` — declared-bound containment as a fault obligation |
| N41 | `precept-language-spec.md:104` — Principle 7, overflow listed with no deferral note |
| N43 | `primitive-types.md:197`, `:236` — "Overflow is a type error", with no cross-reference to `:81`'s disclosure |
| N59 | ADDITION unmet — canon still has no statement of the non-linear rule (want:135). The only `non-linear` hit is an unrelated sentence about UCUM units at `spec:1454` |
| N74 | `temporal-type-system.md:143` — the `(none) | Period path | Duration path` row, still contradicting `:1182` and `:1208` |
| N75 | `primitive-types.md:582` — `notempty` still applies to seven collection types and is still "equivalent to `mincount 1`" |

**Eight of those twelve are in `docs/language/` type documents, and the reason is structural**: the
specification slice states "Nothing outside `docs/language/precept-language-spec.md` was touched," and
no other slice claimed `docs/language/`. `primitive-types.md`, `collection-types.md`,
`temporal-type-system.md` and `business-domain-types.md` were **never opened by any of the four
slices**. The register found rows in them; nobody was assigned to them.

**N75 deserves separate mention.** It is the disposal of the first of phase 1's two *don't remember*
items. The register's § 15 says it is "a row with a resolution and a reason" and "nothing is stalled."
The row is unexecuted, so `notempty` still carries two spellings of one cardinality constraint in
canon, and phase 1's finish-line claim that the item is disposed of rests on a correction nobody made.

**N41 is the cheapest one to fix and the most awkward to leave.** Principles 10 and 11 both gained
"**Overflow is currently exempted from this principle**" in the same commit. Principle 7, six lines
above them in the same list, makes the same claim about the same fault and did not. Three siblings, two
disclosed, one not, in the document the disclosure exists to protect.

### 2.4 Deferrals I checked against "undecidable from canon"

The task asked whether each unexecuted row is genuinely undecidable rather than merely difficult. My
reading:

- **Genuinely undecidable, correctly parked**: N79 (what inspect returns), N80 (`ConstraintPlanIndex`),
  N81 (the induction's own argument), N82 (same-basis period ordering), N57's strategy count, and the
  four philosophy rows. Each needs a decision no canonical document supplies. The `N57` handling —
  removing every bare numeral and stating the disagreement openly rather than picking a number — is
  the right answer to an undecidable question and I would not change it.
- **Decidable, deferred for coherence, and honestly disclosed**: the whole runtime fault-backstop
  family (R9–R14, C1, C2, N22–N30, N34, N35). Both the runtime and compiler records give the same
  reason — the family spans five files and doing two-fifths of it leaves a builder planting annotations
  nothing routes. That reasoning is correct. But the consequence has to be said plainly, and neither
  record says it: **`precept-language-spec.md:110` now states "There are no runtime fault traps" while
  five canonical documents still specify them in operational detail** — a builder pass, a descriptor
  type, an opcode field, an evaluator dispatch check and a named cross-cutting concern. Canon at HEAD
  contradicts itself on a central claim of the guarantee. It is a deliberate, temporary, disclosed-in-
  part state, and it should not survive the night without being on the board.
- **Decidable and simply missed**: the twelve rows in § 2.3.

---

## 3. Checking canon against the want document directly

I read `what-i-want-2026-07-16.md` end to end rather than working from the procedure's twenty derived
claims. The claim list is a fair reduction — I did not find a claim it omits. Two notes on its edges:

- **Claim W12 is narrower in canon than in the want document, by owner ruling, and that is fine.**
  want:143 asks an editable-field write to evaluate "the field's modifier-rules *and* every rule that
  mentions the field." The corrected canon says ingress upholds "the field's own declared contract and
  nothing wider." The register (S1, R3) records walk items 7, 8 and 16 as superseding want:143, and the
  reasoning it gives — that re-evaluating rules reaching the field through a computed field is the
  sweep under another name — holds up. The departure from the want document's literal text is
  deliberate and traceable. Worth knowing it is a departure.
- **Claim W20 really is under-tested, as § 6 admits.** But it is now partly met: `runtime-api.md:258`
  states the premise list *is* the runtime's validation set, "derived from the proof rather than
  maintained as a parallel list alongside it." That sentence and the surviving `ConstraintPlanIndex`
  (N10/N80) are the same open question from two sides, and canon now asserts one side of it while the
  other side's data structure is still specified across two runtime documents.

---

## 4. Disagreements found that are not a row

Six. Two of them are new tonight — a document corrected in one place and left contradicting itself in
another.

### 4.1 `precept-language-spec.md:2085`–`:2094` — construction-time constraint composition

Still reads:

> 2. **Field constraints** (rules, field-level ensures) — post-assignment truth.
> 3. **Global rules** (`rule ...`) — always evaluated.
> 4. **Entry ensures** (`to <InitialState> ensure ...`) … These are the same entry ensures that fire on
>    any transition into the initial state…

This is the post-mutation sweep, restated as the construction model, five layers of it, in the
specification. It contradicts want:199 and it contradicts §0.7 and §3A.4 **in the same document**,
after tonight's edits. The runtime slice corrected the twin of this passage in `runtime-api.md`
(the stateless-construction pipeline table, recorded as an unenumerated site) and fixed the sentence
immediately above it at `spec:2080` — "same ingress validation of the supplied arguments, same guards,
same action chain as any other event". This block, five lines lower, was not reached. **New, no row.**

### 4.2 `precept-language-spec.md:2184` — the PRE0115 construction exemption

> When a precept declares an initial event with at least one construction row … the proof engine skips
> the default-value satisfiability check for the initial state. The rationale: an entity with a
> construction event never inhabits the initial state with only default values — the construction row's
> action chain establishes field values **before any constraint is evaluated**.

Two problems, both created tonight. First, "before any constraint is evaluated" is the removed model.
Second and worse, this states the **whole-precept** suppression as settled design rationale — and the
same commit night rewrote `proof-engine.md:485` and `spec:213` to say the opposite: the exception the
language allows is per field (a default the construction event *provably overwrites*), and the
whole-precept early-return "leaves the base case of the induction open … a prevention hole rather than
a conservative under-emit." The specification now argues both sides, 1,970 lines apart. Row N73 caught
the `proof-engine.md` copy; nothing caught the specification's. **New, no row.**

### 4.3 `compiler-and-runtime-design.md:149`, `:153`, `:169` — the carried-constraint discharge model

`:149`: "The proof engine discharges each obligation by establishing that the operand **carries a
discharging fact** — a declared modifier/rule narrowed into its interval, a `when`-guard fact on the
path, or a statically-known value."

Two things are wrong with that sentence now. It states the discharge model that S11, SC-1 and N65
replaced with proof by induction — and it names **three** premise sources where want:19 and the
corrected `spec:268` name **four**, the missing one being "the rules that already hold in the
pre-state." That is the premise class the want document's `Deposit` case turns on: "*No guard or arg
bound alone gets there*" (want:101). The architecture overview, which `CLAUDE.md` sends every reader
to before pipeline work, describes a proof model that cannot discharge the want document's own second
worked example.

Row N62 covers `:149` and `:165` and is marked "confirmed by reading". The confirmation was correct as
far as it went — the row's question was whether the *provenance* implication survived, and it does not.
Nothing asked whether the *discharge model* in the same sentence survived S11. **New, no row.**

### 4.4 `docs/runtime/README.md:23` — the area map

> 3. [evaluator.md](evaluator.md) — plan execution and **constraint evaluation**

Small, but it is the reading-order map for the seven runtime documents, it sits inside `$CANON`, and it
is what a reader meets before any of the corrected text. **New, no row.**

### 4.5 `graph-analyzer.md:538` — `UnhandledEvent` at Warning

> **Zero coverage** (no state handles the event anywhere) — provably dead declaration. Emit
> `UnhandledEvent` (Warning).

By the test C6's own executed correction wrote into `diagnostic-system.md:190` — *"the test is not
whether the entity stays inhabitable but whether something the author declared can never occur"* — an
event declared and handled nowhere is a declared thing that can never occur, and the document itself
calls it "provably dead." It reads as the event-side twin of the dead row want:155 rejects. Whether it
blocks or reports is a judgement about the language, not a wording fix, so I am not asserting the
answer; I am asserting that C6's reasoning reaches it and no row does. **New, no row.**

### 4.6 `docs/glossary.md` — blind spot 2, checked

Blind spot 2 is almost clean. `docs/README.md` and `docs/agent-onboarding.md` carry nothing normative
about the boundary; the greps that hit everywhere else return nothing there. Two small residues in
`docs/glossary.md`: the **Descriptor** entry lists "fault site" as a runtime identity (`:78`), and the
**Fault** entry (`:113`) defines a fault as "a runtime failure mode … the runtime counterpart of a
compile-time diagnostic". Both belong to the deferred fault-backstop family and should be swept with
it. The procedure's judgement — that a glossary entry is downstream of the spec and gets corrected when
the spec does — is right, provided somebody records that the debt exists. **Recorded here.**

The register's § 14.4 finding on the repository-root `README.md` also re-checks clean at HEAD: it
carries none of the removed vocabulary at all.

---

## 5. Two bookkeeping problems that caused most of the above

**The execution records are keyed to the wrong document.** All four say some version of "every row
below is one of the `S*`/`R*` rows in `sweep-correction-list.md`". The register is the 123-row
document; the correction list is the 44-row input that was folded into it. Thirty-plus register row IDs
appear in **no** execution record — including thirteen that were in fact executed as "sites corrected
that no row enumerates" (N1, N2's site, N4, N5, N6, N7, N21, N68, N69, N72, C8 among them) and twelve
that were not executed at all (§ 2.3). Nothing in the written record distinguishes the two groups, so
the twelve misses are invisible unless somebody runs every pattern. That is what took most of this
re-check.

**The register's commit column is empty for all 123 rows**, and the register's own § 16 says so:
"Rows naming the commit that executed them: 0 — condition 2 is not met and cannot be met here, by
design." That was true when it was written. Four commits have executed rows since and the column is
still empty, so the register at HEAD asserts "Never executed: all 123" against a tree where roughly
half are done. Whoever picks this up next cannot tell executed from missed from the register alone.

---

## 6. The one thing I would put in front of the owner first

**`compiler-and-runtime-design.md:149` — the architecture overview still describes proof by carried
constraint from three premise sources, when the specification it summarises now describes proof by
induction from four.**

Not because it is the largest hole. The `docs/language/` type documents are eight rows and the fault
family is twenty-something. It is first because of what reads it. `CLAUDE.md` names this file the
architecture entry point and `precept-language-spec.md:274` delegates the composition description to
it. Phase 5 writes the discharge procedure. An agent briefed to build against the architecture document
will build a three-premise discharge cascade, will find that `Deposit` does not close, and will
conclude the induction is infeasible — from a sentence sitting a dozen lines below passages the
phase-2 pass rewrote well, and did not rewrite here.

It is also the case that makes the pattern point concrete. The two patterns written to prove that model
gone (`SC-1`, `N65`) both return zero. The model is still there.
