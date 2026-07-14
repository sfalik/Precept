---
title: "Meta-Plan — Making Compiler-Readiness Completeness a Checkable Property"
status: Draft design — owner-gated
date: 2026-07-13
author: Synthesis (Claude, completeness-assurance lens)
owner: Shane
purpose: >
  The methodology for running the compiler-readiness pipeline (spec completeness+consistency →
  gap list → acceptance criteria → TDD tests → code) so that "the readiness plan is complete"
  becomes a CHECKABLE claim rather than a judgment. This is a meta-plan: it designs HOW to run
  the work, with a completeness-verifiable gate at every stage. It does NOT itself produce the
  gap list or the code. It arms an owner decision; it settles nothing.
relationship_to_v3: >
  Widens the target from the v3 MVP-proof-engine subset (compiler-readiness-plan-2026-07-12.md)
  back to the whole-compiler conformance goal, and adds the obligation-creation-completeness
  spine v3 does not carry. Reuses v3's per-slice execution methodology unchanged for the build
  stage — the v3 slices become one band of this plan's denominator, not the whole plan.
---

# Meta-Plan — Making Compiler-Readiness Completeness a Checkable Property

## What this is, in one paragraph

We are going to finish the Precept compiler: make the implementation match the corrected
specification with nothing missing and nothing that falsely claims to prevent a fault. The risk
this document exists to kill is **silent omission** — a spec claim never implemented, a catalog
member never exercised, a place in the code where a division-by-zero can happen but no check is
ever generated. This meta-plan is the machinery that makes "we didn't miss anything" a property
you can **check**, at every stage of the work, rather than a thing you have to trust. Its spine
is a single idea: **build an exhaustive, machine-countable list of everything the compiler must
do (the "denominator"), and make every later stage a total function over that list — every item
dispositioned, none dropped, provable by a count.**

**What this is not.** It is not the gap list (that is the output of Stage 2 below). It is not the
code. It is not a replacement for the existing per-slice build workflow (`lifecycle-4-execute`) —
that workflow runs unchanged at the end and consumes what this pipeline produces. It is the
process that guarantees what it consumes is complete.

---

## Terms used in this document (plain-language, defined once)

This document obeys its own output rule: no coined term is used without a plain-language
definition on first use, and no bare internal code (like "F-LANG-COLL-06" or "Slice 8") carries
load-bearing meaning. Each term below is referred to afterward by the plain phrase, not a code.

- **The denominator** — the complete, machine-countable list of everything the finished compiler
  must do. It is the *whole* against which every later stage's coverage is measured. If the
  denominator is incomplete, every downstream completeness claim is a lie, so building it
  exhaustively is the first and most important move.
- **A total function over a set** — a step that assigns *every* element of its input set an
  explicit outcome, with none left out. "Total" is the checkable property: you count the inputs,
  you count the dispositioned outputs, and the two numbers match. Every stage gate in this plan
  is a total-function check.
- **An audit angle** — one way of enumerating "everything the compiler must do." No single angle
  catches everything; each is exhaustive over its own machine-enumerable set and cross-cuts the
  others, so a miss in one angle surfaces as a hole in another. Five angles are defined below.
- **A completeness gate** — a checkpoint between two stages that refuses to let the next stage
  start until the current stage's output is provably total over its input. Six gates are defined.
- **The prevention guarantee's two axes** — the heart of the product. **Sound** = the compiler
  never falsely says "this cannot fault" for something that can (never a false "proved"). **Complete**
  = at *every* place a fault can happen, the compiler actually generates a check. These are
  different failures needing different machinery, and this plan addresses each with its own gate.
- **A faultable position** — a place in a compiled definition where a runtime fault could occur:
  a division (divide-by-zero), a `sqrt` (negative-input), a collection accessor (empty-collection),
  an arithmetic op (overflow), a bounded write (out-of-range), and the rest of the fault surface.
  The complete list of fault *kinds* is the `FaultCode` catalog (15 members today).
- **An obligation** — the check the compiler generates at a faultable position, demanding the
  operand carry a constraint that makes the fault impossible. "An obligation exists at every
  faultable position" is exactly the completeness axis.
- **A live probe** — a real `precept_compile` run on a crafted `.precept` snippet that
  demonstrates the actual compiler behavior. Every gap finding must carry one. Document-only
  reasoning is rejected (it has demonstrably over- and under-stated the truth in past passes).
- **A work unit** — the atom the pipeline finally emits: a self-contained brief a fresh coding
  agent (with none of this design conversation's context) can pick up and build correctly. The
  readiness plan is a dependency-ordered set of these.

---

## The north star (restated, so the gates have a target)

The readiness plan this pipeline produces must deliver a **complete, sound, spec-conformant,
self-consistent Precept compiler**:

1. **Spec-conformant & complete** — every normative claim in the corrected canonical spec is
   implemented and tested; no undelivered "shall/is/rejects" claim survives.
2. **The prevention guarantee is sound AND complete** — sound (never a false "proved") and
   complete (an obligation at every faultable position; every statically-preventable fault
   actually prevented; no un-obligated fault slot, no dead preventable code).
3. **Compiler/runtime boundary honestly drawn, runtime *contract* fully specified** — the runtime
   is spec-only by design (its evaluator throws "not implemented"); "compiler complete" therefore
   means compiler-code conformance **plus** a complete, self-consistent runtime-contract spec, a
   closed handoff. It does not mean runtime code.
4. **Docs self-consistent** — no contradictions, no drift, no fabricated citations.

Everything below is in service of making each of these four a thing you can *count*, not a thing
you have to *believe*.

---

## The spine: one denominator, five angles, a total-function chain

### Why a denominator at all

The current active plan (the v3 MVP plan) narrowed readiness to seven proof-engine slices grounded
in a set of ruled decisions. That is a fine plan for *those seven slices*, but it has no
denominator — nothing that says "and here is the complete set of things the compiler must do, and
here is the proof these seven cover their share of it." Without a denominator, "complete" is a
judgment. With one, it is a subtraction: `denominator − dispositioned = 0`.

### The five audit angles (each machine-enumerable, mutually cross-cutting)

The denominator is the **union** of five independently-exhaustive enumerations. Each is built by a
mechanical pass over a source that can be counted, so the enumeration's own completeness is
checkable. They overlap deliberately: an item missed by one angle tends to be caught by another,
and the cross-check between angles is itself a completeness signal.

1. **By spec claim** — every normative sentence ("shall / is / must / rejects / is a type error")
   in the corrected language spec and the per-type docs. The spec is ~2170 lines across ~19
   auditable areas (§0.1–§0.8 preamble incl. the graph-analyzer, proof-engine, and guarantee
   contracts; §1 lexer; §2 parser; §3 name-binding/type-checking, ten subsections; §3A
   language-semantics, six subsections; §4/§5 stubs) plus `primitive-types.md`,
   `temporal-type-system.md`, `business-domain-types.md`, `collection-types.md`,
   `catalog-system.md`. This angle *is* the v1 audit's row set, re-derived against the corrected
   spec. It is the primary angle because the spec is the denominator's ground truth (north star 1).

2. **By catalog member** — every member of every catalog, enumerated by a `for-each` over the
   catalog source (`src/Precept/Language/`): Types (~32), Operations (~202), Modifiers (~28),
   Constructs (~14), Diagnostics (~164 `PRE####` codes), Faults (15 `FaultCode` members),
   ProofRequirements (~13), Operators (~21), Functions (~23), Actions (~15), Tokens (~138),
   Constraints (~5), ExpressionForms (15). Because catalogs *are* the machine-readable spec, this
   angle's base set is literally countable from the files, and "every member accounted for" is a
   loop with a counter. It catches the claims the prose spec states loosely or not at all.

3. **By diagnostic code** — every `PRE####` code: is it *emitted* somewhere live (not dead code),
   *tested*, and *spec-described*? The existing `DiagnosticCoverageScanner` (with its three
   emission patterns — the literal-`Create` grep is known-incomplete, so the scanner is the
   authority) and the `PRECEPT0027` diagnostic-emission analyzer already do part of this; this
   angle systematizes it. It catches "the spec names a diagnostic the compiler never actually
   produces" (the v1 audit found several: `PRE0022`, `PRE0088`, `PRE0090` with zero emission sites).

4. **By obligation position** — **the prevention-completeness angle, and the one the current plan
   does not build.** Enumerate every *(expression-form subtype × child sub-expression field)* cell
   — every place one expression can hold another — and cross it with the faultable-operation
   surface. For each cell, ask: does the obligation-collection walk actually recurse into this
   position and generate the obligation the fault there requires? Today the walk (`CollectObligations`
   at `ProofEngine.cs:208–269` and `WalkExpression` at `:275+`) hand-picks which construct entry
   points and which child positions it visits. Two live holes prove the angle is real and currently
   un-covered: a faulting divisor inside a `when`-guard condition generates **no** obligation and
   compiles clean (BUG-031), and a faulting divisor inside a member-call argument (`coll.at(A / B)`)
   generates **no** divisor obligation (BUG-032). There is no catalog of child-expression positions
   (`ChildExpressionPositions` has zero code hits) and therefore no checker proving the walk is
   exhaustive. This angle's denominator is *every faultable position the language can express*, and
   its completeness is the guarantee's completeness axis.

5. **By fault mode** — every `FaultCode` member (15: division-by-zero, sqrt-of-negative, overflow,
   empty-collection-on-access/mutation, out-of-range, length/count-bound, arity, arg-constraint,
   qualifier-mismatch, …). For each: is it marked statically-preventable (wired in
   `StaticallyPreventableMap` to a diagnostic), is an obligation actually *created* for it (angle 4),
   is it *dischargeable or rejecting* (never silently skipped), and is it *tested*? A
   `[StaticallyPreventable]` fault with no live prevention path is a hole in the guarantee (north
   star 2); a fault code with dead preventable code is the inverse hole. The `PRECEPT0002`
   analyzer already asserts every fault code has the attribute wired; this angle extends that to
   end-to-end prevention.

**The master denominator** is these five joined and de-duplicated. Because each base set is finite
and enumerated from source, the *union's* completeness is checkable: you verify each angle's count
against its source (catalog member count matches the file; spec-claim set covers every normative
sentence; diagnostic set is the whole `PRE####` range; obligation-position set is the full
expression-form × child-field product; fault set is all 15 codes), then you verify the join dropped
nothing.

### The total-function chain (the pipeline, restated as coverage arithmetic)

```
corrected spec + catalogs
        │  Stage 1 — spec consistency & scope honesty
        ▼
  the frozen denominator  (5 angles, union, count verified)
        │  Stage 2 — gap analysis: a disposition for EVERY denominator entry
        ▼
  the gap list  (every entry: conformant / gap / divergence / spec-silent→owner / deferred)
        │  Stage 3 — acceptance criteria: ≥1 criterion for EVERY non-conformant entry
        ▼
  the acceptance-criteria set  (each criterion test-shaped)
        │  Stage 4 — TDD: ≥1 failing test for EVERY criterion, red for the right reason
        ▼
  the failing-test matrix
        │  Stage 5 — code: every test green, completeness checkers pass, no regression
        ▼
  a complete, sound, spec-conformant compiler
```

At each `▼` the gate is the same shape: **count the inputs, count the dispositioned outputs, prove
they match.** That is what makes completeness checkable rather than felt.

---

## The two-axis prevention machinery (called out, because it is the heart)

North star 2 has two failure modes that need *different* machinery. The plan builds a checker for
each; conflating them is the trap.

### Axis A — Soundness (never a false "proved")

The compiler must never certify "structurally impossible" for something that can happen. The
failure is **over-proving**: a discharge path that returns "proved/clean" on an input it did not
actually prove. The v3 plan's Slice-0 fail-open sweep addresses exactly this axis — audit every
"maybe-proved" (`bool?`-returning) discharge path so `null`/undecided routes to *reject*, and every
write into the narrowed-interval dictionary so the empty interval (⊥) cannot sneak a
`Contains ⇒ true` over-prove. **This plan keeps that sweep intact**; it is the soundness gate's
core. It adds: a standing **adversarial false-proved hunt** (independent agent, described under
Discovery) that crafts definitions designed to elicit a false "proved," and the **certificate**
(the show-your-work note on every verdict) as the artifact that makes each "proved" re-checkable in
principle. Sound = "no discharge path lets an unproven input pass, and every 'proved' shows work."

### Axis B — Completeness (an obligation at every faultable position)

The compiler must generate a check at *every* place a fault can happen. The failure is **silent
omission**: a faultable position the obligation walk never visits (BUG-031, BUG-032). This axis has
**no checker today** and is the plan's most important new build. The machinery:

1. **A `ChildExpressionPositions` catalog** — declare, per expression-form subtype, the complete set
   of its child sub-expression fields (catalog-before-code: this is language structure, so it lives
   in the catalog, and every downstream consumer derives from it rather than hand-listing). This
   turns "the places an expression can hold another expression" into a machine-enumerable set.
2. **An obligation-position-completeness checker** — a build-time analyzer (in the family of the
   existing `PRECEPT0007` exhaustive-switch and `PRECEPT0027` emission-coverage analyzers) that
   proves, for every subtype, `{declared child positions} − {positions the walk recurses into} = ∅`,
   and for every construct that carries a guard/condition, that the guard is enrolled as a walk
   entry point. This is precisely the systemic fix BUG-032 asks for ("prove no position is ever
   silently skipped again") and the entry-point fix BUG-031 asks for. Once it exists, the two live
   holes are failing tests it turns red, and the whole *family* — not just the two instances
   tripped over — is covered.

Complete = "every faultable position the language can express is visited by the walk and generates
its obligation, provable by a checker, not by inspection." **Sound-but-incomplete** (skips a fault
position) and **complete-but-unsound** (over-proves) are both disqualifying; the two gates are
independent and both must pass.

---

## The stages and their completeness gates

### Stage 0 — Rebuild and reconnect the probe surface (prerequisite)

Every gap finding rests on a live `precept_compile` probe, and the Precept MCP server serves its
*last-spawn build*. So before any gap analysis: rebuild `src/Precept` + `tools/Precept.Mcp`, ask the
owner to `/mcp reconnect precept`, and confirm with `precept_ping` that probes reflect current HEAD.
A stale server produces false gaps (the code is right, the server is old). **Gate 0:** a probe of a
known-current behavior returns the current answer.

### Stage 1 — Spec consistency & scope honesty → freeze the denominator's ground truth

The spec is the denominator's ground truth, so it must be *correct* before it is *counted*. Two
sub-passes:

- **Internal consistency.** Resolve every self-contradiction. The v1 audit already collected a
  "Canon inconsistencies" section (e.g. `sqrt(integer)` — §3.2 says integer widens to number "in any
  context," §3.7 says integer input is a type error; the `choice`-vs-`choice` comparison rule that
  `primitive-types.md` and spec §3.6 state oppositely). Each contradiction is either resolved by the
  owner or explicitly parked with a reason. Contradictions are *not* gaps to implement — they are
  denominator defects, and implementing against a contradictory spec produces incoherent code.
- **Scope honesty.** Mark or remove deferred features so the spec states its *intended end-state*
  truthfully. The spec still carries stale finding-IDs and a "Phase 5 / Specification-only" status
  table in §0.6 (items 7/8/9/10/12) that references a superseded plan; and per-type docs like
  `collection-types.md` are "Canonical design, not yet implemented." The rule (from project memory):
  the spec states what *should* exist; **remove only features we will not build**, never annotate a
  not-yet-built target as absent and never falsely claim a target is implemented. Draw the
  compiler/runtime boundary explicitly here too: which claims the compiler discharges vs. which the
  runtime-contract owns (§0.7 is the seam).

**Gate 1 (spec-consistency gate).** Checkable properties: (a) every contradiction in the v1
"Canon inconsistencies" section plus any new one found has an owner disposition; (b) no stale
finding-ID or superseded-plan reference survives in the spec; (c) every spec claim is tagged
built-target vs. will-not-build, with no claim both. The corrected spec is then **frozen** as the
denominator's ground truth for this cycle — later stages read the frozen version against committed
HEAD (a this-cycle spec edit must not self-validate; grep committed HEAD, not the working tree).

*This stage is where the pre-design/owner gate lives.* A contradiction whose resolution invents new
language surface (a keyword/type/operator/modifier/construct/syntax not already locked) is **not**
resolved inside this pass — it surfaces to the owner as a conversation opener (Tier 2) or, if it
collides with a locked rejection, a heavier conversation quoting the locked decision (Tier 3). Most
of Stage 1 is honesty-correction against locked intent (proceed); the minority that is genuinely new
surface waits for the owner.

### Stage 2 — Gap analysis → a disposition for every denominator entry

The heavy stage. Run the five angles to build the denominator, then disposition **every** entry
against the implementation, each backed by a file:line pointer **and** a live probe. Dispositions
(the taxonomy every entry lands in exactly one of):

- **Conformant** — implementation matches the spec claim; probe confirms; no work.
- **Gap** — spec claim not implemented / not tested; the honest missing work. → feeds Stage 3.
- **Divergence** — implementation contradicts the spec claim (behavior differs). Check git history
  first: an AI-co-authored departure from locked spec is cleanup (build to spec), not a decision to
  re-litigate. → feeds Stage 3, or Stage 1 if it exposes a spec defect.
- **Spec-silent / contradictory → owner** — the disposition reveals the spec doesn't actually say,
  or says two things. Routes back to Stage 1's owner conversation, not to the build.
- **Intentionally deferred** — out of this cycle's scope by owner decision, with a named re-open
  trigger (a defer with no trigger is disallowed).

**Discovery discipline (finding the tail).** Completeness of *this* stage — that no gap was missed —
is the plan's hardest sub-problem, so it gets the most machinery:

- **Loop-until-dry.** Re-run the gap pass until a full pass finds *zero* new gaps. The first pass
  finds the obvious; the tail lives in the passes that find "only" one or two more. Stop when a pass
  is empty, not when the finder is tired.
- **Multi-modal, one angle at a time.** Run each of the five angles as its own dedicated pass
  (by-spec-area, then by-catalog-member, then by-diagnostic-code, then by-obligation-position, then
  by-fault-mode). An item invisible from the prose angle (a diagnostic the spec names loosely)
  surfaces from the catalog angle; a fault position invisible from the spec surfaces from the
  obligation-position product. The disagreement *between* angles is a completeness signal — chase it.
- **Adversarial false-proved hunt (soundness tail).** A separate, independent pass whose only job is
  to craft definitions that try to make the compiler falsely say "proved" — the Axis-A tail. This is
  the pass that found the fail-open holes; it runs to dry here too.
- **v1 reconciliation as backstop.** The prior audit (619 rows, six buckets: 383 conformant, 85
  partial, 20 stub, 53 missing, 78 diverges) is joined into the new denominator (it has no stable
  IDs, so join by classification + claim + citation). **Every one of its ~236 non-conformant rows
  must map to a new denominator entry with a disposition** — this is the backstop that catches a
  regression in the *new* pass ("the old audit found this; did we?"). A v1 row with no new-pass
  match is either a genuine miss (add it) or a since-closed item (mark it closed, with the probe
  proving closure). The v1 audit is effectively untouched (~0 of 236 closed), so this is real work,
  not a formality.

**Gate 2 (gap-coverage gate).** Checkable properties: (a) every denominator entry has exactly one
disposition; (b) every non-conformant disposition carries a file:line pointer *and* a live probe
(document-only findings rejected); (c) the last gap pass added zero entries (loop-until-dry
satisfied); (d) every v1 non-conformant row maps to a dispositioned new entry. The count
`denominator − dispositioned = 0` is the gate.

### Stage 3 — Acceptance criteria → a criterion for every gap

Every non-conformant entry (gap / divergence) gets ≥1 **acceptance criterion**: a plain-language,
test-shaped statement of the behavior that must hold, derived from the *corrected* spec (not from an
agent's memory of it), with a worked example. "Test-shaped" means a failing test can be written
directly from it. For obligation-position and fault-mode entries, the criterion enumerates the whole
*family* (every numeric/length/count/qualifier × shape × source cell), because the recurring failure
is criteria that cover only the instance tripped over, not the family (this is a standing project
lesson).

**Gate 3 (acceptance-criteria-coverage gate).** Checkable properties: (a) every gap/divergence entry
maps to ≥1 criterion; (b) every criterion is test-shaped and cites its corrected-spec source by
stable reference; (c) family entries enumerate every cell, each with an explicit expected outcome.
`gaps − criteria-covered = 0`.

### Stage 4 — TDD → a failing test for every criterion

Every criterion becomes ≥1 failing test *before* any code, as a `# EXPECT:`-contracted diagnostic
sample under `test/integrationtests/diagnostics/` plus a full-compile assertion
(`Compiler.Compile(...)` / `CompileExpectingError`, **never** the type-checker-only `Check`/`CheckExpectingClean`
helpers — those skip the proof engine and produce false greens on exactly the diagnostics these
criteria touch). Each test must be **red for the right reason** (fails because the behavior is
absent, not because the test is malformed) — verified by running it against current HEAD.

**Gate 4 (test-coverage gate).** Checkable properties: (a) every criterion has ≥1 test; (b) every
new test is red against HEAD before code, and the red is the expected failure. `criteria − tested = 0`.

### Stage 5 — Code (the existing build workflow, unchanged)

The failing-test matrix + the corrected docs + the acceptance criteria are exactly the input the
existing per-slice build workflow (`lifecycle-4-execute`) consumes: enumerate/probe → failing-test
matrix first (already done, Stage 4) → gate → delegate to a fresh git-worktree coding agent →
adversarial diff review → integrate → pause at the slice boundary for owner review. **This plan does
not reinvent that workflow** — it feeds it a certified-complete, decomposed work-unit set.

**Gate 5 (conformance gate).** Checkable properties: (a) every Stage-4 test green; (b) the two
completeness checkers pass — the obligation-position-completeness analyzer (Axis B) and the
diagnostic-emission/fault-prevention coverage (angles 3 and 5); (c) the soundness sweep is complete
(every `bool?` discharge routes `null→reject`; every dict-write guards ⊥); (d) `dotnet build` green
including all existing Roslyn analyzers; (e) no regression (full corpus green). `tests − green = 0`
and both checkers green.

---

## The output contract: what the pipeline emits (agent-executable AND owner-legible)

The pipeline's product is the **readiness plan**: a dependency-ordered set of **work units**. The
entire point is that the owner starts fresh autonomous coding agents on them. So each work unit is
built to two co-equal, complementary standards — and legibility and agent-executability are *the same
property viewed twice*: plain language + clear structure + defined terms make a unit simultaneously
reviewable by the owner and correctly executable by a context-free agent. Coined jargon and
label-soup propagate as implementation errors for the agent exactly as they obscure review for the
owner.

Each work unit is:

- **A self-contained brief** — carries everything a context-free agent needs: its failing tests /
  acceptance criteria (the definition-of-done it checks itself against), exact corrected-spec
  references, source-reuse pointers by file:line (reuse existing catalog/code — never fork a parallel
  definition), and its scope boundary. The artifact *is* the prompt; reliance on this conversation's
  accumulated context is a defect.
- **Agent-sized** — completable by one coding agent in one bounded run. Checkable proxy: touches a
  bounded file set, has a self-contained failing-test matrix, and closes a coherent gap-cluster (one
  catalog family / one obligation-position family / one spec subsection). Oversized → split.
- **Dependency-ordered and parallelizable** — sequenced where dependent, independent where not, so
  multiple worktree-isolated agents run concurrently without conflict. (The obligation-position
  checker and the `ChildExpressionPositions` catalog are a hard prerequisite for the Axis-B units, in
  the same way v3's Slice 0 is a prerequisite — foundational substrate lands first.)
- **Self-verifying + reviewed** — embedded failing tests (the agent knows when it is done);
  adversarial review of the agent's diff before integration.
- **Owner-legible** — readable and reviewable in plain language: every coined term defined on first
  use, **no bare internal code as structure** (refer to each unit by a plain description of what it
  does, not "Unit 12" or a finding-ID), concrete worked examples for behavioral claims, scans cleanly
  for a human. This is co-equal with agent-executability, not traded against it.

**Gate 6 (decomposition-completeness gate).** The work-unit set is checkable as a partition of the
gap list: (a) every gap maps to exactly one work unit (no orphan gap, no gap in two units with
conflicting scope); (b) the union of all units' gaps equals the full gap set; (c) every unit passes
the agent-brief checklist (self-contained, agent-sized, tests embedded, reuse pointers present,
plain-language); (d) the dependency graph is acyclic and marks the parallelizable frontier. This gate
is what certifies "the readiness plan is complete": the plan covers every gap, every gap traces to a
denominator entry, and the denominator was verified total over all five angles. Completeness is thus
a chain of counts from work unit back to spec claim.

---

## Sequencing (what runs when)

```
Stage 0  rebuild + reconnect MCP (prerequisite; minutes)
Stage 1  spec consistency & scope honesty  ──▶ Gate 1  ──▶ FREEZE denominator ground truth
Stage 2  five-angle gap analysis (loop-until-dry, adversarial hunt, v1 reconcile) ──▶ Gate 2
Stage 3  acceptance criteria (family-complete) ──▶ Gate 3
Stage 4  failing tests (red-for-right-reason) ──▶ Gate 4
Gate 6   decompose into work units; certify the partition  ──▶ the readiness plan
── owner reviews and signs off the readiness plan in plain language ──
Stage 5  fresh coding agents build, per lifecycle-4-execute, unit by unit ──▶ Gate 5 per unit
```

Stages 1–4 + Gate 6 are the meta-plan's own work (produce the readiness plan). Stage 5 is the
readiness plan being executed. The owner sign-off sits at Gate 6 — the plan is legible *because* the
gates forced plain-language, spec-traced, worked-example units.

Within Stage 2, the five angles run as five dedicated passes; the by-obligation-position and
by-fault-mode passes depend on the `ChildExpressionPositions` catalog being *specified* (not yet
built) so the position product is enumerable — specifying that catalog is the first Axis-B task and
gates the two prevention angles.

---

## Model and independence strategy

- **Stage 0** (rebuild/reconnect) — main loop; mechanical.
- **Stage 1** (spec consistency) — main-loop-orchestrated, owner-in-the-loop for every contradiction
  resolution and every new-surface question. The main loop drafts corrections; the owner rules. Not
  delegated to an independent agent, because the rulings are the owner's.
- **Stage 2** (gap analysis) — **the heavy stage: Opus, extensive, not Fable** (cost — Fable is
  reserved for independence-critical verification, and gap-finding is breadth work the main model does
  well). Each angle is a dedicated pass. The **adversarial false-proved hunt** and a **completeness
  critic that re-runs each angle to dry** are the exceptions that *do* want independence — run those as
  a fresh independent agent (Fable) so the finder is not the same context that drew the denominator
  (a fork would launder the main loop's own blind spots back as "verified").
- **Stage 3–4** (criteria, tests) — mechanical-to-moderate; drafting acceptance matrices from a clear
  corrected spec is Haiku/Sonnet-tier; the family-enumeration completeness check wants one independent
  pass (does the matrix cover every cell, or only the ones tripped over?).
- **Gate 6** (decomposition) — main-loop-authored, one independent legibility+partition review
  (Fable): can a context-free agent build each unit, and does the set partition the gap list?
- **Stage 5** (build) — fresh worktree coding agents per `lifecycle-4-execute`; no Fable on the
  mechanical build; adversarial diff review (Fable) on soundness-critical units.

The through-line (from project memory): the main loop **orchestrates and verifies against source**;
it is never the last word on a fact or a completeness claim. Every gate's count is verifiable by a
reviewer re-running the enumeration, and the independence-critical passes (adversarial hunt, tail
critic, legibility review) live *outside* the main loop by construction.

---

## How "the readiness plan is complete" is proven (the completeness certificate)

The claim is discharged by a chain of counts, each independently checkable:

1. **Denominator total over its sources** — each angle's count matches its source enumeration
   (catalog member counts match the files; the spec-claim set covers every normative sentence; the
   diagnostic set is the whole `PRE####` range; the obligation-position set is the full expression-form
   × child-field product; the fault set is all 15 codes). (Gate 1 + the Stage-2 angle counts.)
2. **Every denominator entry dispositioned** — `denominator − dispositioned = 0`, each non-conformant
   entry probe-backed, loop-until-dry satisfied, v1 rows all mapped. (Gate 2.)
3. **Every gap → criterion → test → green** — three total-function joins, each a count. (Gates 3–5.)
4. **Both prevention checkers green** — the obligation-position-completeness analyzer proves the walk
   visits every faultable position (Axis B); the soundness sweep proves no discharge over-proves (Axis
   A). (Gate 5.)
5. **The work-unit set partitions the gap list** — every gap in exactly one unit, union equals the
   whole, each unit an executable + legible brief. (Gate 6.)

"Complete" is then not a judgment: it is `0 = 0` at six gates, re-runnable by the owner or a reviewer.

---

## Where this deliberately diverges from the current (v3) plan, and why

1. **Target: whole compiler, not the proof-engine MVP subset.** v3 narrowed readiness to seven
   proof-engine slices and left the broader completeness backbone un-owned; the ~236 non-conformant
   rows of the prior audit are effectively untouched. This plan restores the whole-compiler
   conformance goal (north star 1). *Rationale:* "readiness" that ships a sound proof MVP over an
   otherwise-unconformant compiler is not the north star. *Tradeoff accepted:* a larger, longer
   pipeline than the MVP. *The v3 slices survive* as one band of the denominator (the prevention
   guarantee's Axis-B/Axis-A entries), and v3's per-slice execution methodology is reused unchanged
   at Stage 5.

2. **Adds the obligation-creation-completeness spine (Axis B) v3 does not have.** v3's fail-open sweep
   is discharge-*soundness* (Axis A) — it fixes over-proving. It does *not* address obligation
   *creation* — whether an obligation exists at every faultable position. BUG-031 and BUG-032 are live
   holes in exactly that axis, and no checker for it exists (`ChildExpressionPositions` = 0 hits).
   *Rationale:* a prevention engine that never *generates* a check at a fault position is incomplete
   regardless of how sound its discharge is; the guarantee (spec §0.7: "at *every* fault-prone
   operation, an obligation") demands the creation axis. *Precedent:* the existing `PRECEPT0007` /
   `PRECEPT0027` build-time analyzers show the catalog-driven-completeness-checker pattern already
   works here. *Tradeoff accepted:* a new catalog + analyzer to build before the Axis-B units.

3. **Constructs an exhaustive, multi-angle, machine-countable denominator; v3 has none.** v3 works
   from a set of ruled decisions with no denominator — "complete" is a judgment about whether the
   seven slices are the right seven. *Rationale:* completeness is only checkable against a whole; the
   five angles make the whole countable from source. *Tradeoff accepted:* building the denominator is
   itself substantial work (the heavy Stage 2).

4. **Reconciles the v1 audit *in* as a backstop rather than setting it aside.** v3 leaves the prior
   audit at ~0 of 236 closed. *Rationale:* the prior audit is the single best guard against the new
   pass regressing (missing something already found); dropping it discards that guard. *Tradeoff
   accepted:* the join is manual (no stable IDs in the v1 format — join by classification + claim +
   citation).

5. **Makes every stage a total function with a count-based gate, so "complete" is checkable.** v3's
   gates are per-slice acceptance matrices (good, but local); this plan adds the *global* coverage
   arithmetic — denominator minus dispositioned equals zero at each stage — so the *whole* is provably
   covered, not just each slice internally sound. *Rationale:* the north star is a property of the
   whole compiler, so the completeness proof must be global. *Tradeoff accepted:* more bookkeeping (the
   counts) in exchange for a checkable rather than felt completeness claim.

---

## Honest limits (what this plan does NOT guarantee)

- **Runtime code.** The runtime is spec-only by design; "compiler complete" here means compiler-code
  conformance + a complete, self-consistent runtime-*contract* spec. This plan does not build the
  evaluator. The runtime-contract-completeness check is angle 1 applied to `docs/runtime/*.md` (all
  currently spec-only/stub) — a closed handoff, not running code.
- **Spec correctness beyond consistency.** Stage 1 guarantees the spec is internally *consistent* and
  *scope-honest*; it does not guarantee the spec's design decisions are *right*. New-surface questions
  and locked-decision overrides route to the owner (the pre-design gate), which this plan honors but
  does not shortcut.
- **The denominator is only as complete as its five angles.** The multi-angle cross-check and the v1
  backstop make a missed item unlikely (a miss must evade all five angles *and* not appear in the prior
  audit), but "unlikely" is not "impossible." The loop-until-dry and the independent tail critic are
  the mitigations; the residual risk is stated, not hidden.
- **This meta-plan settles nothing.** It arms an owner decision on how to run the work. The owner may
  rescope (keep the MVP subset), reshape the angles, or reject the whole-compiler target. The gates are
  a proposal for how to make completeness checkable, not a ruling that the work must be done this way.
