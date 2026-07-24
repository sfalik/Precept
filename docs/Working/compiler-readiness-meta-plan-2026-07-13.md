---
title: "Meta-Plan — Methodology for a Complete-Compiler Readiness Plan (reconciled)"
status: Draft — owner-gated
date: 2026-07-13
author: Synthesis (Claude), reconciled from three lens-designs + a 3-judge adversarial scorecard, grounded against committed HEAD
owner: Shane
purpose: >
  The methodology for running the readiness pipeline (spec completeness+consistency → gap list →
  acceptance criteria → TDD tests → code) so that "the readiness plan is complete" becomes a
  CHECKABLE claim, not a judgment. This is METHODOLOGY — it does not itself produce the gap list
  or the code. It arms an owner decision; it settles nothing.
supersedes_for_scope: >
  Widens the 2026-07-12 v3 plan (compiler-readiness-plan-2026-07-12.md) from a proof-engine MVP
  subset back to the whole compiler. The v3 slices are kept, refiled as a subset of one build
  layer, and re-sequenced onto a completeness substrate they currently lack. See § 11.
reconciles: >
  meta-plan-design-execution-2026-07-13.md (spine),
  meta-plan-design-completeness-2026-07-13.md (five-angle denominator; behavioral false-Prove hunt),
  meta-plan-design-conformance-2026-07-13.md (re-extraction totality gate; bidirectional no-orphan traceability).
---

> **Current status / where-are-we is tracked in [`compiler-readiness-STATUS.md`](compiler-readiness-STATUS.md) — read it first.** This doc owns the whole-compiler methodology spine; the STATUS page owns current state.

# Meta-Plan — Methodology for a Complete-Compiler Readiness Plan

## What this document is (and is not)

**Is.** The recipe for running the readiness work so that each stage's completeness is *mechanically
verifiable* before the next stage consumes it, and so the artifact that comes out — the readiness
plan — decomposes into work units a **fresh, context-free coding agent** can build correctly and the
**owner** can read and sign off in plain language.

**Is not.** It is not the gap list, not the acceptance criteria, not the code, and not a second
proof-engine design. It produces none of those; it produces the *discipline and gates* under which
they get produced.

**The one-sentence spine.** Turn "is the readiness plan complete?" from a human judgment into a chain
of mechanical closure checks — one per denominator (the whole spec, the prevention guarantee, the
runtime handoff) — where the highest-leverage question, prevention-*creation*-completeness, is enforced
by a **set of three build-time checks** (the walk reaches every position, the position set is complete
against the record shapes, and every faultable operation attaches its obligation) so that "no fault
position was left un-obligated" is answered by `dotnet build` going red, not by an auditor's confidence
— while the parts that *cannot* be made mechanical (an arithmetic false-Prove that returns a wrong
non-null "proved") are named as **stated residuals** with property-based testing and a standing
independent behavioral hunt as their mitigation, never certified closed.

---

## How to read this document

A handful of terms carry real weight from the first page on. Defined in plain language, once, here:

- **Faultable position.** Any spot in a `.precept` definition where a runtime fault could occur — a
  division, an array index, an overflow-prone arithmetic step. The compiler's job is to make sure none
  of these can blow up at runtime.
- **Faultable operation.** A specific catalog operation (an arithmetic operator, a built-in function, a
  collection access) that *can* fault unless something proves it safe — e.g., `/` can fault on a
  zero divisor, `sqrt` can fault on a negative input. Every faultable operation is the thing an
  obligation gets attached to, wherever it appears as a faultable position.
- **Obligation.** A proof check the compiler attaches to a faultable position (like a division) so it
  can prove the fault can't happen there. If the position carries no obligation, nothing was ever
  checked — that's a silent hole, not a proof.
- **Discharge.** To *discharge* an obligation is to prove it holds — i.e., to show the fault really
  can't happen for every value the definition allows. An obligation that's created but never
  discharged, or discharged wrongly, is exactly the failure this methodology is built to catch.
- **The walk.** The compiler's traversal of a definition's expressions, during which it visits every
  faultable position and collects (creates) the obligations attached there. "Does the walk reach every
  position?" is the first of the three creation-completeness checks in § 4.3.
- **Fable.** An independent model used for review and hunt passes (e.g., the behavioral false-Prove
  hunt in § 4.4). It runs outside the main line of work — a separate agent, not a fork of the session
  doing the design or the build — so its judgment isn't biased by having produced the thing it's
  reviewing.

**The four north-star outcomes.** The purpose of the whole pipeline (front matter, above) is a
complete, sound, spec-conformant compiler, legible end to end. Concretely, that cashes out as four
outcomes, referenced elsewhere in this doc as "north-star item N":

1. **Spec-conformance** — every normative claim in the corrected spec is either implemented and tested,
   or explicitly dispositioned as deferred/out-of-scope. This is denominator A (§ 1).
2. **The prevention guarantee** — no faultable position is left un-obligated, and no obligation's
   discharge ever falsely says "proved" when it isn't. This is denominator B, "the heart" (§ 1, § 4).
3. **Runtime-contract-completeness** — every fault the compiler hands off to the runtime is named and
   dispositioned in the runtime-contract spec, with nothing dangling. This is denominator C (§ 1).
4. **Docs self-consistency** — the documentation describing all three of the above doesn't contradict
   itself or the code it describes. Not a fourth coverage denominator to enumerate against, but a gate
   condition riding on all three: nothing above counts as closed while its own documentation still lies
   about it (§ 1).

With those in hand, the rest of the document should read without needing to hunt for a definition.

---

## 1. The core reframing — one pipeline, three denominators, five enumeration angles

The owner's stated pipeline is right:

> spec completeness+consistency → complete gap list → comprehensive acceptance criteria → TDD tests → code

The load-bearing correction is that **"the gap list" is not one list against one denominator.** The
north-star outcome has three structurally different completeness questions, each with its own
denominator (the set you must cover), its own way of proving coverage, and its own verification
technique. Collapsing them into one flat gap list is exactly how the v3 plan lost the completeness
backbone: it counted proof *capabilities* and never counted fault *positions* or spec *clauses*.

| Denominator | The set to cover ("everything") | "Complete" means | Proven by | Primary verification |
|---|---|---|---|---|
| **A. Spec-conformance** | Every normative clause in the corrected canonical spec + per-type docs + catalogs (see § 5's five angles) | Every clause maps to a passing conformance test or an owned gap-unit | `denominator − dispositioned = 0` at each handoff | source read at file:line + live `precept_compile` probe |
| **B. Prevention guarantee** (the heart) | Two orthogonal axes — see § 4 | (i) *creation:* every faultable position carries an obligation; (ii) *discharge:* no path ever false-Proves | (i) **three build-time checks** (walk-reachability ∧ carrier-enumeration ∧ attachment) all go green; (ii) a shape-limited sweep + property-based testing + a **standing behavioral hunt** with a named residual | build-time checkers + `Compiler.Compile` regression suite + independent hunt |
| **C. Runtime-contract-completeness** | Every fault the compiler hands off + every runtime-contract obligation | Every handed-off fault is named in the runtime-contract spec and either proved-at-compile-time or explicitly runtime-enforced; no dangling capture | `handoff-set − contract-set = 0` | spec cross-read + fault-catalog cross-read |

**Docs self-consistency (north-star item 4)** is not a fourth denominator — it is a *gate condition*
riding on all three: a gap-unit does not close until its doc-touch obligations land in the same
commit (the project's existing doc-sync rule), and the spec-correction stage (§ 3) removes the
contradictions up front so the denominator itself is consistent.

**Denominator A is not enumerated by one reading.** The single biggest completeness risk is that the
enumeration of "every spec claim" is itself short — a claim no reader's angle ever reaches survives
every downstream check because it was never on the list. So denominator A is built as the **union of
five independently-exhaustive enumeration angles** (§ 5), each countable from a different source, so a
miss must evade *all five* and the v1 backstop. This is the wide-denominator move; it is load-bearing
and it is why "we enumerated carefully" is never accepted as the gate.

---

## 2. The failure modes this methodology is built to prevent

Not hypothetical — each is a thing this project has done; each maps to a specific structural move
below, so the design can be checked against its purpose.

| Failure mode (observed) | Structural prevention (where) |
|---|---|
| **Un-owned work falls between slices** (v3's narrowing left the completeness backbone with no owner; holes get found one probe at a time) | The three closure denominators (§ 1) + the three build-time creation-completeness checks (§ 4) make "un-obligated position" a red build, not a missed audit. Every gap-unit has exactly one owner in the ledger (§ 5). |
| **Planning-loops with no code** (sessions circle re-deciding; no commits for weeks) | Gates are *artifacts that compile / a checker that runs / a count that equals zero*, not review meetings. Discovery and spec-correction are **time-boxed with an explicit "ready to code" exit** — never an open-ended "loop until the finder is tired" (§ 3, § 5.3). |
| **Rework where slices re-touch the same files** | Layer-2 units are partitioned by **disjoint file ownership** (§ 7); two concurrent worktree agents never edit the same file. Genuinely-shared files collapse into one serial unit. |
| **False-green tests that skip the proof engine** (`Check` / `CheckExpectingClean` bypass the proof stage) | Acceptance criteria are `Compiler.Compile(...)` / `CompileExpectingError` only, enforced per unit and checkable by grep at review (§ 6). |
| **A probe that shows "no diagnostic" read as "proved safe"** (when it actually means "no obligation created" — a false-Prove) | Every covered position carries a **positive obligation-creation probe**: a `.precept` that puts a known fault in that position and asserts the obligation *is raised* — not merely "compiles clean" (§ 4.4, § 6). |
| **Enumeration covers only the instance tripped over** (design-time family under-enumeration) | Each gap-unit expands to the **whole input family** with every cell disposed, and every `[StaticallyPreventable]` code it touches is verified live-not-dead (§ 6). |
| **Redefining the bar to pass** (an unprovable case reframed as an "acceptable conservative boundary") | Creation-completeness is closed by three mechanical build-time checks, not a narrative; an undeclared position is a red build. "Unprovable = reject" is mechanical (§ 4). The arithmetic-false-Prove residual is *named as an open hole with a mitigation*, never marked N/A (§ 4.4, § 10). |
| **Certifying "complete" while unsound work slips through** (presenting a shape-limited sweep as a proof of no-false-Prove) | § 10 certifies only what each mechanism actually proves. The discharge sweep is stated as shape-limited; the arithmetic-false-Prove class is a **stated residual**, not a closed cell. |
| **Mistaking an engineering wall for an identity crisis** (a slice hits a boundary; the reflex is to re-litigate "what should Precept be" instead of diagnosing the wall — this cost roughly a month in the June→July arc, per Frank's 2026-07-13 retrospective) | **Engineering-diagnosis-first rule (§ 7.4).** At any hard slice, the *first* artifact is a one-page engineering diagnosis of the boundary — what is genuinely undecidable, what is merely-unimplemented, and what one authored rule would discharge it. Re-opening design or product identity is allowed *only if that diagnosis fails to close* — which, on the 2c-ii evidence, it usually will not. |

---

## 3. Stage 0 — Prerequisite: rebuild the probe surface, then correct and freeze the spec

### 3.0 Rebuild and reconnect the MCP (minutes; gate: a known-current behavior probes correctly)

Every gap finding rests on a live `precept_compile` probe, and the Precept MCP serves its
*last-spawn build*. Before any gap analysis: rebuild `src/Precept` + `tools/Precept.Mcp`, ask the
owner to `/mcp reconnect precept`, confirm with `precept_ping`, and verify a probe of a known-current
behavior returns the current answer. A stale server manufactures false gaps (the code is right, the
server is old). After any later `src/Precept` change, reconnect again before re-probing.

### 3.1 Spec correction (the denominator is fixed first) — TIME-BOXED

**Why first.** The corrected spec is the denominator for A and the reference for B and C. Deriving a
gap list from a spec that still contradicts itself or still claims-implemented what the code doesn't
do propagates the drift into the tests and then the code. Fix the ruler before measuring.

**Three passes, in order:**

1. **Scope-honesty.** The spec states the *intended end-state* (features built or not). The axis is
   intended-vs-not and status-honesty, **not** built-vs-not. So: *remove* only features Precept will
   not build; *mark* status honestly where a doc claims "Implemented" but code disagrees (e.g. the
   Status line "§5 Proof Engine complete" is an overclaim against the live soundness holes — correct
   it); never *falsely claim* a target is implemented; never annotate a real intended-but-unbuilt
   target as absent. Concrete inbound items: `collection-types.md` is "Canonical design, not yet
   implemented" (keep as intended, mark honestly); §0.6 items 7–10 marked "Specification-only" yet
   the v3 structural-severity work says item 7 now rejects (reconcile the drift). Strip embedded
   transient scaffolding from normative prose (`F-LANG-*` finding IDs, a "Phase 5" tracking column,
   dated status tables) into commit history — the spec is not a project tracker.

2. **Internal consistency.** Resolve the spec's own contradictions. The v1 audit already collected a
   `## Canon inconsistencies reported` section (e.g. `sqrt` integer-arg: §3.2 widening "in any
   context" vs §3.7 "integer input is a type error"; choice field-vs-field: `primitive-types.md`
   "always an error" vs spec §3.6 "allowed under same-element-type"). Each contradiction is either a
   **mechanical drift fix** (proceed — one side is plainly stale; check git history: AI-co-authored
   departures are cleanup, not decisions) or a **genuine fork** (two defensible readings → owner
   conversation gate; *not* settled inside this pass). A claim register built on a self-contradictory
   spec produces unsatisfiable acceptance criteria — two tests that cannot both pass — so this pass is
   a hard precondition, not a warm-up.

3. **Compiler/runtime boundary consistency.** Verify §0.7 (guarantee contract) and the
   runtime-contract docs draw the same line: every fault class the compiler declares
   `[StaticallyPreventable]` is prevented at compile time; everything else is explicitly a
   runtime-governance obligation. Enumerate the runtime contract as a *closed handoff* — every fault /
   constraint class dispositioned to exactly one side (compile-time / runtime-ingress /
   outside-envelope), none claimed by both or neither. This needs no runtime code; it feeds
   denominator C.

**Owner-consultation gate inside the stage.** Most edits are mechanical drift fixes (Tier 1 —
proceed). A genuine fork, or any edit touching *new* language surface (keyword/type/operator/modifier/
construct/syntax), is Tier 2/3 — surface it in plain text with the prior locked decision quoted
verbatim, and wait. The spec-correction stage **cannot** introduce or settle new surface unilaterally.
A genuinely-new-surface item is marked `needs-design`, pulled *out* of the conformance track into a
separate `/design` track, and does not enter the gap ledger as an implementable claim until closed.

**Freeze.** The corrected spec is committed and tagged as the conformance denominator; the owner signs
the diff. Nothing downstream is derived until this commit exists. All later stages read the **frozen,
committed HEAD** version — a this-session spec edit must not self-validate (grep committed HEAD, never
the working tree).

**Time-box.** This stage is drift-correction, not redesign. It has an explicit exit (the freeze gate
below). If it starts generating new design, that is a signal to stop and route the specific item to
`/design`, not to hold the stage open. An unbounded spec-correction stage is the planning-loop
failure this project has already lived.

**Freeze gate (mechanical) — deliberately shallow-but-bounded, not a full audit.** There is a real
tension here: verifying *every* "Implemented" claim against code clause-by-clause **is** a large slice
of the conformance audit, and pulling it into a stage the plan insists must stay small would balloon
Stage 0 into the very planning-loop it exists to avoid. The resolution: Stage 0 checks Status-field
honesty **shallowly** — at the granularity of "does the feature this Status describes exist and run at
all" — and **defers per-clause "Implemented" verification to the Stage 1 five-angle pass** (§ 5), where
angle 1 already re-checks every claim against source at file:line. The freeze commits a *scope-honest*
spec (no feature marked Implemented that is wholly absent, no intended feature deleted), **not** a
fully-audited one; the audit-grade check lives in the stage built for it.
- Every doc Status field is scope-honest against code at the shallow "exists-and-runs" granularity —
  no wholly-absent feature marked Implemented, no intended-but-unbuilt feature removed. Per-clause
  Implemented-vs-code verification is explicitly a Stage 1 angle-1 obligation (§ 5), not a freeze gate.
- The `## Canon inconsistencies` set is fully dispositioned: each fixed, or an open owner-fork with a
  recorded neutral two-sided framing.
- No stale finding-ID / superseded-plan reference survives in normative prose.
- The §0.7 / runtime-contract boundary reads consistently for every `[StaticallyPreventable]` fault,
  and every fault/constraint class is dispositioned to exactly one side.

---

## 4. The prevention spine — two axes, two kinds of proof

This is the sharpest divergence from v3 and the heart of the whole guarantee. The prevention guarantee
(north-star item 2) has **two orthogonal completeness axes that fail independently**, and — critically
— they demand **different machinery**, because one is closeable by a compiled invariant and the other
is not.

### 4.1 The two axes

- **Creation completeness** — is an obligation *created at all* at every faultable position? A missing
  obligation is a false-Prove: the position compiles clean because nothing was ever checked. Verified
  against HEAD: `CollectObligations` (`ProofEngine.cs:208`) enrolls `.Condition` (rules/ensures,
  `:239/:247`), `ComputedExpression` (`:256`), and action inputs (`:491/:493`) as walk entry points —
  but never `.Guard` (BUG-031: every faulting arithmetic in a `when` guard collects zero obligation →
  false Proved). `WalkExpression` (`:275`) is a hand-written `switch (expr)` (`:282`) whose
  `TypedMemberAccess` arm (`:319`) recurses `ma.Object` (`:322`) but not `ma.Arguments` (BUG-032), and
  the switch has **no `default:` arm** — a newly-added `TypedExpression` subtype silently escapes the
  walk entirely. `ChildExpressionPositions` has **zero code hits**: no checker enforces the walk
  covers every child position. **v3 does not cover this axis at all**, and it mis-files BUG-031/032
  into its discharge sweep — a mis-file, because *auditing discharge paths will never surface a
  position that has no discharge path.* This axis is closeable by build-time checks — but by
  **three** of them working together, not one (§ 4.2–4.3): the walk must reach every position, the
  set of positions the walk is told to cover must itself be complete against the record shapes, and
  each faultable operation must actually attach its obligation upstream. All three, or the axis is
  not closed.

- **Discharge soundness** — *given* an obligation was created, does its discharge path ever return
  "proved" on an input it has not actually proved? v3's fail-open sweep (`bool?`-return sites +
  narrowed-interval dict-writes) addresses part of this. But that sweep's denominator is a *shape*
  (null-routing + ⊥-writes), not the whole space of ways to false-Prove: an interval-arithmetic bug
  that returns a *wrong non-null* "proved" result (e.g. a sign error yielding `[0,0]` treated as
  nonzero) passes the sweep, passes every static checker, and compiles clean. This axis is **not**
  fully closeable by a static invariant; it needs a behavioral hunt and a stated residual (§ 4.4).

**Sound-but-incomplete** (skips a fault position) and **complete-but-unsound** (over-proves) are both
disqualifying; the two gates are independent and both must pass. Neither mechanism alone guarantees
"no false-Prove."

### 4.2 Creation completeness — a field-derived denominator over the whole reachable object graph, so the ruler cannot be short

The trap all three source designs share (and every one of the three judges flagged): a checker that
proves `{declared child positions} − {walked positions} = ∅` merely moves BUG-032 up one level — a
*forgotten catalog declaration* reproduces the exact false-Prove, now hidden behind a **green** build.
Reducing the guarantee to "did a human declare every child position correctly?" is the bug the checker
exists to kill.

**So the denominator is derived mechanically from reality — the actual record structure — not
hand-authored.** But it must be derived over the **right** set of records, and the naive scoping —
"every expression-typed field on every expression subtype" — is itself short. An expression can be
reachable through a record that is **not** an expression subtype, and reflection scoped to expression
subtypes structurally cannot see it. Three verified carrier classes escape the naive scope:

1. **Child positions on expression subtypes (the case the naive scope does catch).**
   `TypedMemberAccess` (`SemanticIndex.cs:106`) carries `Object` (an expression field, walked) and
   `Arguments` (an `ImmutableArray<TypedExpression>` field, **not** walked). A field-enumerating pass
   flags `Arguments` as an unwalked position — catching BUG-032 by construction.
2. **Entry-point carriers — the walk's *roots*, which are not expression subtypes.** The obligation
   walk's roots are enrolled by a hand-written list in `CollectObligations`
   (`ProofEngine.cs:208–270`); the guard condition `.Guard` is an `TypedExpression? Guard` field on
   the transition-row, event-row, rule, ensure, access-mode, and state-hook records
   (`SemanticIndex.cs:526, 571, 582, 592, 600, 612`) — **none of which is an expression subtype.**
   Reflection scoped to expression subtypes cannot enumerate `.Guard` at all. BUG-031 (guard
   arithmetic collects zero obligation → false Prove) lives exactly here: the plan's own flagship bug
   sits *outside* the naive denominator. A future construct that adds a guard- or condition-bearing
   root and forgets to enroll it is a green build and a false-Prove — the same "enumerate only the
   instance you tripped over" failure (§ 2), reproduced one level up at the roots.
3. **Wrapper carriers — records that hold an expression but are not expressions.**
   `TypedHoleSegment` (`SemanticIndex.cs:158`) extends `TypedInterpolationSegment`, not
   `TypedExpression`, and carries a `TypedExpression Expression`; `TypedInterpolationSlot` (`:193`) is
   the same shape (a plain record with an `Expression` field). A faulting expression reachable only
   through an interpolation segment or slot lives behind a carrier the expression-subtype graph never
   reaches.

**So the denominator is "every `TypedExpression` / `TypedExpression?`-typed field (including
`ImmutableArray<TypedExpression>`) on *any record reachable from an obligation root*" — expression
subtypes, guard/root carriers, and wrapper carriers alike — not "on every expression subtype."** It
is enumerated by reflection / Roslyn over the reachable record graph in `SemanticIndex.cs`, with no
human deciding which records count. This is the field the § 4.3 checks measure against.

### 4.3 Creation completeness — three mechanized checks, not one; built as a REFACTOR, not an additive body-scanning analyzer

Creation completeness has three independent ways to fail, so it needs three checks, not one. The walk
can skip a position it *does* reach the root of; the set of positions the walk is told to cover can be
short against the record shapes; and a position the walk faithfully visits can carry no obligation
because the type-checker never attached one. A single reflection check closes only the middle failure.
All three below must be green.

First, the shared build discipline. Budgeting this as "a new sibling analyzer that reads which child
properties a hand-written `switch` recurses into" under-states it dangerously: proving recursion-targets
inside a per-arm switch body (`ProofEngine.cs:275–364`, no `default:` arm) is genuinely hard static
analysis, and a fresh agent who builds it as a brittle body-scanner produces a *false-green source at
the heart of the guarantee.* Verified against HEAD, no existing analyzer does this: `Precept0019`
checks marker-attribute presence, `Precept0027/0028` check call-shape presence with allow-lists — all
check **presence**, none reads recursion out of a switch. So the walk-reachability piece is built as a
**refactor of the obligation walk** (checks 1 below), and the enumeration pieces as **presence-only**
Roslyn checks (the easy static analysis, checks 2 and 3).

**Check 1 — walk-reachability, by refactoring the walk to iterate declared positions (kills "declared ≠
walked" by construction).** Replace the hand-written per-arm recursion with a walk that iterates a
catalog-declared set of child positions (`ChildExpressionPositions`) and recurses into each — *and*
enroll the walk's **roots** the same way, from a declared set of the entity-carrier expression fields
(`.Condition`, `.ComputedExpression`, action inputs, **and `.Guard`**). Once both the roots and the
recursion *are* the iteration over declarations, a declared fault-reachable position the walk skips is
impossible — the sets are identical by construction. BUG-031's guard root and BUG-032's `ma.Arguments`
child position are the refactor's first two failing tests. The `default:`-less switch's silent-escape
of a new subtype is closed by the same refactor.

**Check 2 — carrier-enumeration, a field-derived presence checker over the whole reachable graph (kills
"declared ≠ reality").** A Roslyn analyzer (sibling to `Precept0019`, presence-check only) enforces
that every `TypedExpression` / `TypedExpression?`-typed field on **any record reachable from an
obligation root** (§ 4.2 — expression subtypes, guard/root carriers *and* wrapper carriers, not just
expression subtypes) has a `ChildExpressionPositions` declaration marked either *fault-reachable* or
*N/A-with-reason*. An undeclared such field — a new expression subtype, a new guard-bearing record, a
new interpolation carrier — is a **red build**. This is the reflection-level exhaustiveness check that
closes the "the catalog itself is short" hole. Its denominator is the reachable-record graph, so it
sees `.Guard` and the interpolation carriers that a subtype-scoped reflection would miss.

**Check 3 — attachment-completeness, so a *visited* position actually carries its obligation (kills
"walked but empty").** Reaching a node is not the same as that node bearing the right requirement.
Attachment happens *upstream* in the type-checker, before the walk runs: the operator path reads
`resolvedOperation.ProofRequirements` from the catalog (`TypeChecker.Expressions.cs:683`), and other
sites hand-code `ProofRequirements: ImmutableArray<ProofRequirement>.Empty`
(`TypeChecker.Expressions.Callables.cs:364, 374`). A faultable operation whose catalog entry omits its
requirement, or a hand-coded `Empty` where one is owed, yields a node the walk visits, finds empty, and
creates nothing — a false-Prove behind a green build. So: a presence check that **every faultable
`Operations` / `Functions` catalog member declares a `ProofRequirements` value (or N/A-with-reason)**,
plus **every hand-coded `Empty` attachment site enumerated and justified**. (The two verified `Empty`
sites at `Callables.cs:364, 374` sit on error paths — the malformed-action and unknown-action arms,
which already emit `TypeMismatch` and produce an error-typed node — so they are legitimate
N/A-with-reason; the check *records* that justification rather than trusting the site.)

**Check 4 (Layer 3) — no dead preventable code.** Prove every `[StaticallyPreventable]` fault code has
at least one *live obligation-creation* site (not merely an emission site — `Precept0027` already
checks emission). A preventable fault with no creation path is a false-Prove-by-omission and a red
build. (The v1 audit found `PRE0022`, `PRE0088`, `PRE0090` with zero emission — this makes the *class*
detectable, not just the instances.) **Honesty guard:** a code that cannot be made to emit is an open
hole to flag, not a cell to quietly mark N/A.

Together, creation completeness = **walk-reachability ∧ carrier-enumeration ∧ attachment-completeness**
(with dead-code as a fourth guard): reality (all reachable record fields, plus catalog requirement
declarations) → declaration (the position catalog + requirement catalog) → walk (iterates the
declarations and their roots). Each link closed mechanically, a red build on any break, no human
judgment in the chain. Once all are live, creation-completeness is closed by `dotnet build`, and every
capability slice built afterward **inherits the guards for free** — provided all four are wired; the
guarantee is exactly the conjunction of the three creation checks (plus the dead-code guard), no
stronger.

### 4.4 Discharge soundness — the shape-limited sweep, positive creation probes, and a standing behavioral hunt

This axis is **not** certified closed by any static check. Its machinery, honestly bounded:

- **The fail-open sweep (retained, named shape-limited).** Every `bool?`-returning discharge path
  routes `null`/undecided → reject; every write into the narrowed-interval dictionary guards the empty
  interval (⊥) so `Contains ⇒ true` cannot sneak an over-prove. This is v3's Slice-0 work, kept. It
  covers null-routing and ⊥-writes — **it is not a proof of no-false-Prove**, because its denominator
  is a shape, not the space of wrong-but-non-null "proved" results.

- **Positive obligation-creation probes.** For every *covered* position, a `.precept` that puts a
  known fault there and asserts the obligation *is raised* — distinguishing "correctly proved safe"
  from "no obligation created." A probe showing "no diagnostic" is consistent with *both*; only a
  positive must-emit assertion on a known-faultable input tells them apart.

- **Property-based / metamorphic testing of the discharge arithmetic (partly-mechanical, shrinks the
  residual).** Before falling back to a human hunt, subject the interval / length / qualifier
  arithmetic to generated-input testing: generate large numbers of random *valid* definitions and
  assert soundness invariants on the narrowed intervals the discharge engine produces — monotonicity
  (narrowing only ever tightens), containment (the narrowed interval contains every value the concrete
  operation could yield), and metamorphic relations (equivalent definitions produce compatible
  verdicts). A generated definition that violates an invariant is a mechanically-surfaced candidate
  false-Prove. This is a *stronger, partly-mechanical* mitigation than a human crafting counterexamples
  by hand, and it shrinks the residual below what a human hunt alone reaches — though it does not close
  it (the invariants are asserted, not exhaustively proved).
- **A standing, independent, behavioral false-Prove hunt.** A fresh independent agent (Fable, outside
  the main loop) whose only job is to *craft definitions designed to elicit a false "proved"* — an
  arithmetic bug returning a wrong non-null result, a narrowing sign error, a composition that
  over-proves — targeting the classes generated testing is weakest at (semantically-loaded shapes a
  random generator rarely hits). Together with the property-based pass it catches a discharge-arithmetic
  false-Prove behaviorally rather than assuming false-Proves only manifest as null-routing or ⊥-writes.
  It runs as a discovery loop, time-boxed (§ 5.3), and re-runs as a standing regression whenever
  discharge arithmetic changes.

- **The stated residual (do not certify closed).** The class "an interval/length/qualifier arithmetic
  bug returns a wrong non-null 'proved'" is **not** closed by any of the above with certainty — the
  sweep is shape-limited, the property-based pass asserts invariants rather than proving them, and the
  behavioral hunt is a search, not a proof. § 10 names this as an open residual with the property-based
  and behavioral mechanisms as its mitigation. Presenting any of them as a mechanical proof of
  discharge-soundness is the overclaim this synthesis explicitly refuses.
- **Why this residual is genuinely hard — the missing oracle.** The reason discharge-soundness cannot
  be closed the way creation can is worth naming plainly: there is **no executable oracle to test
  against.** Closing it by differential testing would mean comparing "the compiler said proved-safe"
  against "the operation did not actually fault at run time" — but there is nothing to run that
  produces the second answer. This is a build-order fact, not a design limitation: the residual is a
  *consequence* of the discharge oracle not yet existing, and any consumer that builds on this
  guarantee inherits whatever the property-based pass and the search did not reach. § 11 surfaces this
  to the owner as a first-class decision, not a footnote.

**Substrate-before-capability.** The § 4.3 refactor + the three checks are built *before* any capability
slice: they are the completeness gate those slices are measured against, and they guard them as they land.
Building capabilities first and the checker later means re-auditing every capability against the
checker after the fact — rework by construction.

---

## 5. Stage 1 — The gap ledger — five angles, count-gated, traced both ways, single-owned

The "complete gap list" stage produces one artifact: a **gap ledger**. Its completeness must itself be
provable, it must survive being consumed by context-free agents, and it must be legible to the owner.

### 5.1 The five enumeration angles (denominator A, built wide)

Denominator A is the **union of five independently-exhaustive, machine-countable, mutually
cross-cutting angles.** No single angle catches everything; each is exhaustive over its own countable
set; a miss in one tends to surface as a hole in another, and cross-angle disagreement is itself a
completeness signal to chase.

1. **By spec claim** — every normative sentence ("shall / must / is / rejects / is a type error") in
   the corrected spec and per-type docs. The *primary* angle (the spec is ground truth) and the one
   most prone to an under-counted denominator — so it gets the strongest completeness backstop
   available (§ 5.2), which is honestly less than a totality proof.
2. **By catalog member** — every member of every catalog (`src/Precept/Language/`): Types (~32),
   Operations (~202), Modifiers (~28), Constructs (~14), Diagnostics (~164), Faults (~15),
   ProofRequirements (~13), Operators (~21), Functions (~23), Actions (~15), Tokens (~138),
   Constraints (~5), ExpressionForms (15). A `for-each` with a counter; catches claims the prose
   states loosely or not at all.
3. **By diagnostic code** — every `PRE####` code: emitted live (not dead), tested, spec-described.
   Use `DiagnosticCoverageScanner`'s three emission patterns as the authority (the literal-`Create`
   grep is known-incomplete); catches "the spec names a diagnostic the compiler never produces."
4. **By obligation position** — every faultable position (the field-derived set of § 4.2, over the
   *whole reachable-record graph* — expression subtypes, guard/root carriers, wrapper carriers),
   crossed with the faultable-operation surface (which faultable catalog operation attaches which
   requirement). This angle *is* the creation-completeness denominator; its completeness is the § 4.3
   trio of checks (walk-reachability, carrier-enumeration, attachment-completeness), not a hand list.
5. **By fault mode** — every `FaultCode` member: is it statically-preventable-wired
   (`StaticallyPreventableMap`), obligation-*created* (angle 4), dischargeable-or-rejecting (never
   silently skipped), tested? Extends `Precept0002`'s attribute check to end-to-end prevention.

The master denominator is these five joined and de-duplicated. Each angle's count is verified against
its source; then the join is verified to have dropped nothing.

### 5.2 The spec-claim angle's completeness backstop — agreement + lint + structural floor, not a totality proof (the piece the other angles get from source)

Angles 2–5 are countable from source (catalog files, `PRE####` range, record fields, `FaultCode`
members), so their enumeration completeness is a subtraction. Angle 1 (prose claims) is the one with
no natural source-count — so it gets two mechanical gates instead of a "we read carefully" judgment:

- **Modal-verb lint + declarative-normative extension.** A lint over the frozen spec accounts for
  every `shall / must / rejects / is-a-…-error` sentence. **Extended** — because declarative-normative
  definitions carry normative weight *without* a modal verb ("a quantity is a number paired with a
  unit") — to also flag definitional/declarative sentences in normative sections. *Honest bound:* this
  extension has **no clean mechanical boundary** — "a quantity is a number paired with a unit"
  (normative) and "this section is organized as follows" (not) are both declarative `is`-sentences, and
  telling them apart is reading for meaning, the human judgment the gate wants to replace. So the
  extended lint degrades to "surface every declarative sentence in a normative section, then a human
  keeps or drops each." Its modal-verb core is a hard mechanical anchor; its declarative extension is a
  surfaced candidate set a human dispositions, not a self-certifying count.
- **Independent re-extraction diff.** A *fresh independent agent* (not a fork — a fork launders the
  extractor's own reading) re-derives the claim register from the frozen spec; the two extractions are
  diffed and reconciled to zero, and the register's claim-set must equal the lint's hit-set. To keep
  the diff from drowning in segmentation noise (one agent splits a compound "shall" into three rows,
  the other keeps one), reconcile **at section-anchor granularity first, then sentence** — the
  section anchor is the stable join key, so two readings converge per-section before per-sentence.
  *Honest bound:* this proves **agreement, not coverage.** Two independent agents can share a blind
  spot — a normative claim stated only in a table cell, a worked example, a footnote, or a diagram
  caption — and omit it from *both* extractions; the diff is then zero and the clause is silently
  outside the denominator forever. A zero diff means "the two readers agree," not "nothing was missed."
- **Structural coverage floor (the check the two readers cannot both dodge).** So angle 1 adds a
  structural pass over the frozen spec's document structure: **every normative section, and every table
  and worked-example block inside one, must yield at least one extracted claim or carry an explicit
  "no normative content" marking.** An un-extracted table cell or example block is then a **red gate**,
  not an invisible omission — a shared blind spot in a structural block is caught by the block having
  produced nothing, even when both readers walked past it.

Taken together these are the strongest available anchor for angle 1 — the modal-verb lint (mechanical),
the re-extraction diff (agreement), and the structural coverage floor (per-block presence). But state
it honestly: this is an **agreement-plus-lint-plus-structural-floor backstop, not a totality proof.**
The residual it cannot close is a normative claim that is *both* buried below the structural granularity
(inside prose a block-level check counts as "has content") *and* missed by both readers. That residual
is smaller than "walk clause by clause" / "loop until dry" leaves, and it is named, not certified away.

### 5.3 Discovery is time-boxed, not looped-until-dry

Re-running each angle until "a pass finds nothing" is a *stopping heuristic*, not a totality proof:
zero-new-in-one-pass does not entail zero-remaining, and on a ~2650-line surface with five angles it
is also the unbounded pre-code sink this project has already lived. So:

- The **mechanical gates are the completeness proof** — the five angle-counts against source (§ 5.1),
  the re-extraction diff + lint + structural coverage floor (§ 5.2), the three creation-completeness
  checks (walk-reachability, carrier-enumeration, attachment — § 4.3), the count-gate (§ 5.4), and the
  v1 backstop (§ 5.5). These are what certify "nothing missed."
- **Loop-to-dry is retained only as a supplementary discovery aid**, time-boxed with an explicit
  "ready to start coding" exit, never as the completeness claim. The behavioral false-Prove hunt
  (§ 4.4) and the adversarial empty-cell pass likewise run to a time-box, then hand off; a dry pass is
  a *signal*, the gates are the *proof*.

### 5.4 Row shape, count-gate, and bidirectional no-orphan traceability

Each ledger row carries everything a fresh coding agent and the owner need:

- **Spec-clause anchor** — the exact normative sentence + stable citation (e.g. `precept-language-spec.md
  §3.3, "No implicit fallback… emits a diagnostic and assigns ErrorType"`). The join key to
  denominator A: the spec's own section anchor + the quoted claim. **No coined `F-LANG-*` / `OQ1` /
  `D-3` scheme** — those are the transient labels the project bans; the anchor+sentence is the one
  identity that is stable, legible to the owner, and traceable for an agent at once.
- **Source evidence** — the file:line where the code does (or fails to do) the thing, read against
  **committed HEAD**.
- **Live probe** — a `precept_compile` result against the freshly-rebuilt MCP, quoted verbatim.
  Document-only rows are rejected — this session's brief proved twice that document-only analysis both
  over- and under-states. For obligation-position rows, the probe is a **positive creation probe**
  (§ 4.4): assert the obligation *is raised* on a known fault, not merely "compiles clean."
- **Classification** — the v1 six-bucket scheme (fully / partial / stub / missing / diverges) plus,
  for divergences, drift-vs-decision (check git history — AI-co-authored departures are cleanup).
- **Denominator + disposition** — which of A/B/C; and one of *implement-against-locked-spec* (proceed),
  *owner-fork / needs-design* (new surface or genuine contradiction — conversation first), or
  *out-of-scope-with-trigger* (deferred, with its re-open condition; never a bare defer).
- **Owner** — the single work-unit that will close it. **No row is owner-less; no row has two owners.**

**Count-gate at the handoff (denominator − dispositioned = 0).** Every angle's denominator entry has
exactly one disposition; the two numbers match; the gate is a count, not a reviewer's satisfaction.

**Bidirectional no-orphan traceability.** The chain is `spec claim → acceptance criterion → test →
code`, and **no orphans in either direction**: forward, every claim reaches a criterion→test→code (or
is dispositioned conformant-already-tested / deferred-with-trigger); backward, every criterion / test /
behavioral change names the claim it serves — a change with no claim is undocumented surface (add a
claim) or invented scope (remove it). A backward orphan is how scope creep and invented requirements
enter; keying on the section-anchor + quoted-claim makes both directions countable *and* legible.

#### Worked example — one fully-instantiated row

Every field above, filled in for one real spec claim (run this session, against committed HEAD):

- **Spec-clause anchor.** `primitive-types.md:373` — "`decimal` and `number` never mix implicitly.
  Every operation that combines `decimal` and `number` — arithmetic, assignment, comparison, function
  arguments, default values — is a **type error**. The author must use an explicit bridge function."
- **Source evidence.** `TypeChecker.Expressions.cs:1027-1046` (`ResolveBinaryOp`). The resolver tries,
  in order: an exact catalog match, then the widening ladder (`TryResolveBinaryWithWidening`), then a
  catalog-without-operation match, then a context retry — no step ever pairs `decimal` with `number`
  because no such catalog entry exists. When all four fail, line 1042-1046 emits
  `DiagnosticCode.TypeMismatch` (`PRE0018`) naming both operand types. This is a **passive** absence
  (no catalog entry to find), not an active rejection rule — the diagnostic fires because nothing
  matched, which is exactly what the spec claim requires.
- **Live probe.** `precept_compile` against:
  ```
  precept LaneMixTest

  field A as decimal
  field B as number

  rule A + B > 0 because "test"
  ```
  returned (verbatim, trimmed to the load-bearing diagnostic):
  `{"code":"PRE0018","message":"Expected a decimal value here, but got 'number'"}` — confirming the
  type error fires exactly where the spec says it must.
- **Classification.** Fully — implemented, and the live probe matches the claim.
- **Denominator + disposition.** A (spec-conformance); disposition = *conformant-already-tested* — the
  claim already reaches a passing behavior, so no new criterion/test/work-unit is created for it.
- **Owner.** None — a *conformant-already-tested* row closes at the gap-analysis stage itself; there is
  no work-unit to assign, only the record of who ran the check (the Opus gap-analysis pass, § 5.5) and
  what it found.

### 5.5 v1 reconciliation — a regression-catcher, not a totality proof

The v1 audit (619 prose-bullet rows, no stable IDs, buckets: fully 383 / partial 85 / stub 20 /
missing 53 / diverges 78, plus its `## Canon inconsistencies` section) is a month stale and pre-dates
this session's fixes. **It is a backstop against under-derivation, not a proof of totality** — it is
one prior *incomplete* enumeration, so it catches a regression *from v1's own coverage*, never a
never-found omission. Join each v1 non-`fully` row (by anchor + claim + citation) against the fresh
ledger; every v1 row lands in exactly one bucket: **re-derived** (fresh ledger has it),
**closed-since** (a probe shows it now passes — record the closing commit), or **superseded**
(Stage 0 spec correction removed/changed the clause). A v1 row that is *none of these* is a miss in
the fresh derivation — go find why. **The join is manual best-effort, not a mechanical count:** v1's
rows are prose bullets with no stable IDs, so matching them onto the fresh ledger's section-anchor +
quoted-claim keys is a reading judgment, not a subtraction. That is acceptable for a backstop — but it
means v1 reconciliation is scored as *coverage assurance*, never quoted as a count-clean gate. It
cannot be the sole backstop for any angle; the five-angle union + the § 5.2 completeness backstop carry
the completeness claim as far as it honestly goes (with angle 1's named residual, § 5.2).

**This gap-analysis stage is the heavy, Opus-extensive pass** (per the constraints): source-grounded,
probe-verified, on the model tier that can hold the whole spec and reason across it — not Fable (cost),
not a cheap model, not document-only. The independence-critical sub-passes (re-extraction diff,
behavioral false-Prove hunt, tail critic) run as fresh independent agents *outside* the main loop.

---

## 6. Acceptance criteria → failing tests (TDD: test-first, family-complete, full-compile)

Each gap-row becomes acceptance criteria, then a **red** test suite, before any code.

- **One gap-row → one input-family matrix (not one instance).** Each row expands to its whole input
  family, every cell explicitly disposed. For a prevention gap: `{numeric / length / count / qualifier}`
  × `{fault-prone shapes}` × `{sources — literal / field-ref / arg / computed / guard / reject-row}`.
  For a spec-conformance gap: the enumerated surface the clause governs. **Family-completeness gate:**
  every cell has a disposition — a failing test, a passing test, or a recorded N/A with a reason.
- **Tests written RED first, as executable definition-of-done.** Each should-emit cell → an
  `# EXPECT:`-contracted diagnostic sample under `test/integrationtests/diagnostics/*.precept`
  (drift-guarded), plus a full-compile assertion. Each should-discharge cell → a clean-compile
  assertion. Each obligation-position cell → a **positive creation probe** (§ 4.4).
- **Full-compile only.** Every assertion drives the whole pipeline — `Compiler.Compile(...)` /
  `CompileExpectingError` — **never** the type-checker-only `Check` / `CheckExpectingClean`, which skip
  the proof engine and produce false greens on exactly these diagnostics. Checkable by grep at review.
- **`[StaticallyPreventable]` liveness.** For every preventable code the unit touches, a test asserts
  it actually emits on the violating cell — closing the "dead preventable code" class the § 4.3
  no-dead-code check detects structurally. A code that cannot be made to emit is an open hole to flag, not an
  N/A cell.
- **Red for the right reason.** Each new test fails against HEAD because the behavior is absent, not
  because the test is malformed — verified by running it before code.

**Gate (before any code):** every gap-row has a family-complete RED matrix; every matrix is
full-compile-based; every touched preventable code has a liveness test; every criterion cites its
corrected-spec anchor and names the claim it serves (backward no-orphan). Count: `criteria − tested = 0`.

---

## 7. Work-unit shape and dependency-ordered sequencing

The readiness plan the pipeline outputs is a set of **work units**. This is the contract each unit
meets and the order they run — what makes the plan executable by fresh coding agents without rework,
and legible to the owner.

### 7.1 The self-contained agent brief (the artifact IS the prompt)

Each unit carries, in plain language:

- **What it does** — a plain-language description, used as its name. No `W-A` / `Slice-8` /
  `F-LANG-COLL-06` / `D-3` labels as structure. Coined terms defined on first use or written in plain
  prose. The owner reads the unit and knows what it delivers.
- **Its RED suite** — the family-complete failing tests (§ 6), the definition-of-done.
- **Exact canonical spec references** — the anchors from its gap-rows (stable citations only: spec §,
  catalog file, stage doc — never a `docs/Working/` path or a finding ID).
- **Source-reuse pointers (file:line)** — the existing catalog entries and code seams to *reuse*,
  never fork. New code is a thin wiring layer over existing catalog/machinery; a parallel definition is
  a review failure. (The v3 §3-money unit is the model: "attach `IntervalTransfer` to the op-meta
  family via catalog wiring — zero engine code.")
- **Its scope boundary** — the disjoint file set it owns, and what it explicitly does not touch.
- **Doc-touch obligations** — the canonical docs it updates in the same commit (per the CLAUDE.md
  routing table).

**Agent-sized.** One unit = one coding agent, one bounded worktree run. Checkable proxy: bounded file
set, self-contained failing-test matrix, one coherent gap-cluster. Oversized → split.

#### Worked example — one fully-instantiated work-unit brief

Grounded in a real, currently-open gap (verified against committed HEAD, this session): the money and
quantity multiplication/division operations in the Operations catalog carry proof requirements for
divisor-safety (e.g., "divisor must be non-zero"), but **none of them carry an `IntervalTransfer`** —
unlike the plain `integer`/`decimal`/`number` lanes, which all do. `ProofEngine.Intervals.cs:105-111`
falls through to `NumericInterval.Unbounded` whenever a `BinaryOperationMeta` has no `IntervalTransfer`,
so today a rule like `TotalCost <= '1000 USD'` where `TotalCost = AvgCost * Quantity` cannot be proved
even when `AvgCost` and `Quantity` both carry tight bounds — the multiplication result is treated as
unbounded, not narrowed. This doesn't produce a false-Prove (the proof engine stays conservative and
correctly reports "cannot prove"); it's a completeness gap — a rule the author's bounds should let the
compiler prove, but can't yet.

- **What it does.** Wire interval narrowing through money/quantity multiplication and division, so the
  proof engine can narrow a computed money or quantity result the same way it already narrows plain
  numeric arithmetic.
- **Its RED suite.** The family is `{money, quantity}` × `{×, ÷}` × `{decimal-scaling, same-unit ratio}`:
  `Money × decimal`, `Money ÷ decimal`, `Money ÷ Money` (same currency), `Quantity × decimal`,
  `Quantity ÷ decimal`, `Quantity ÷ Quantity` (same dimension). Each cell is a `.precept` with bounded
  input fields, a computed field holding the product/quotient, and a `rule` whose bound the narrowed
  interval should let the compiler prove — e.g. `field AvgCost as money in 'USD' max '100 USD'`,
  `field Quantity as integer max '10'`, `field TotalCost as money in 'USD' computed AvgCost * Quantity`,
  `rule TotalCost <= '1000 USD' because "..."`. Today this fails to compile clean (the rule is
  unprovable); it goes green once the cell's operation carries an `IntervalTransfer`. Divisor-can-be-zero
  behavior is unchanged and out of the RED suite — those `ProofRequirements` already exist and already
  pass.
- **Exact canonical spec references.** The Operations catalog entries themselves are the operative spec
  for this behavior (catalogs are the language specification — see `docs/language/catalog-system.md`);
  the conceptual arithmetic model they implement is described in `business-domain-types.md` §"money"
  (line 477) and §"quantity" (line 632). Cross-currency and cross-dimension results are described in
  `business-domain-types.md:1046-1061` ("Compound Types and Dimensional Cancellation") — explicitly out
  of scope for this unit (see below).
- **Source-reuse pointers (file:line).** `Operations.cs:1332-1338` — the existing
  `AddTransfer`/`SubtractTransfer`/`MultiplyTransfer`/`DivideTransfer` functions, already wired for the
  `integer`/`decimal` lanes (`Operations.cs:109-117`, `:140-148`, `:202-219`). Reuse these directly —
  no new transfer function needed — on `MoneyTimesDecimal` (`:437`), `MoneyDivideDecimal` (`:442`),
  `MoneyDivideMoneySameCurrency` (`:452`), `QuantityTimesDecimal` (`:538`), `QuantityDivideDecimal`
  (`:543`), and `QuantityDivideQuantitySameDimension` (`:553`), since each entry's qualifier/currency
  compatibility is already checked separately by its existing `ProofRequirements` — only the magnitude
  arithmetic needs a transfer function, and that arithmetic is identical to the plain-decimal case.
  `ProofEngine.Intervals.cs:102-112` (`IntervalOfNarrowed`'s `TypedBinaryOp` arm) is the single
  consumption point and already reads `IntervalTransfer` generically off any `BinaryOperationMeta` — it
  needs no change. Zero engine code, matching the v3 §3-money-unit model cited above.
- **Its scope boundary.** Owns only the six named `Operations.cs` catalog entries (add
  `IntervalTransfer = MultiplyTransfer` / `DivideTransfer`) plus their new test cells. Does **not** touch
  `MoneyDivideMoneyCrossCurrency`, `QuantityDivideQuantityCrossDimension`, or the Period/Duration/Price
  divide-family entries — those produce a different result type (`exchangerate`, a compound `quantity`)
  and need their own transfer-function design, out of this unit's file set. Does not touch
  `ProofEngine.Intervals.cs` (already generic) or any existing `ProofRequirements` (divisor-safety is
  unchanged).
- **Dependency.** Sits in Layer 2 (§ 7.2), on top of Layer 0's creation-completeness trio and Layer 1's
  discharge-soundness correction — both must already be green, since this unit's tests assert on the
  narrowed-interval discharge path those layers guard. Runs beside the other Layer-2 proof-capability
  units but serializes with any other unit also touching `ProofEngine.Intervals.cs` or this region of
  `Operations.cs` (the disjoint-file rule, § 7.2).
- **Doc-touch obligations.** `business-domain-types.md`'s money/quantity arithmetic description gains a
  note that these six operations now participate in interval narrowing (previously silently unbounded).
  No `diagnostic-system.md` change — this closes a completeness gap, not a new fault class.
- **Definition-of-done.** Every cell in the family matrix is green under `Compiler.Compile`; no cell is
  document-only; the diff touches only the six catalog entries and their tests (confirming the
  "zero engine code" reuse claim); the doc-touch obligation above lands in the same commit.

### 7.2 The dependency layers (substrate → soundness → parallel capability+conformance → closure)

Chosen to eliminate rework — each layer is a stable base the next builds on:

- **Layer 0 — Substrate (serial; blocks everything).** (a) The verdict/certificate shape — the
  `ProofVerdict` three-way DU + certificate format + `CertificateSteps` catalog (v3's Slice 0, kept
  intact — every capability's tests assert on the verdict shape). (b) The **creation-completeness trio**: the
  obligation-walk refactor (iterating declared child positions *and* declared roots, guards included) +
  `ChildExpressionPositions` catalog + field-derived reflection checker over the whole reachable-record
  graph + the catalog-attachment / hand-coded-`Empty` check + the Layer-3 dead-code check (§ 4.2–4.3).
  BUG-031 (guard root) and BUG-032 (member-argument child position) are the refactor's first failing
  tests. *Rationale: every downstream unit either asserts on the verdict shape or must not reintroduce
  an un-obligated position; both guards must exist first.*
  **Two must-fix-before-Slice-0 items (Frank's 2026-07-13 charter-gate review — Slice 0 is the highest
  recurrence risk, the June "2c-ii" analog):** (1) **Fold the `DeadEndState` Warning→Error flip INTO
  Slice 0.** Slice 0 relabels dead-end rows claiming outcome-neutrality "now that `DeadEndState` =
  Error," but that flip is delivered by the structural-severity slice — so building Slice 0 first as
  written changes corpus outcomes the plan swears are neutral. (2) **Rule OQ1 (the certificate
  step-kind for the multi-term match) before Slice 0's definition-of-done depends on it** — it is on
  the critical path with no ruling logged. Do not start Slice 0 with either open, and treat Slice 0
  the way 2c-ii should have been: full input-space enumeration + adversarial soundness review before
  commit + refuse any "while we're here" scope.

- **Layer 1 — Discharge-soundness correction (mostly serial).** The fail-open holes on *already-walked*
  obligations + the `bool?`-and-dict-write sweep, plus standing-up the behavioral false-Prove hunt
  (§ 4.4). *Rationale: capability slices extend `BuildNarrowedIntervals`; correct the base before
  adding capability on top.*

- **Layer 2 — Capabilities + spec-conformance gaps (parallel *across* the two families, largely serial
  *within* the proof family; partitioned by disjoint file ownership).** Two families run concurrently:
  (i) the v3 proof capabilities — bounded money/quantity arithmetic, conditional/event-input narrowing,
  the witness gate + family grid, multi-term single-fact, constant-rule-to-bounds — refiled as
  capability units *on top of* the Layer-0 guard they previously lacked; (ii) the non-proof
  spec-conformance gaps from denominator A — exponent literals resolving to zero, choice-default
  membership, temporal `date ± '30 days'` resolution, the dead diagnostic codes (PRE0022/0088/0090),
  CI-flag enforcement, and the rest of the ~236 open v1 rows re-derived fresh. *Honest parallel
  frontier: the conformance-gap family (ii) touches disjoint file sets and parallelizes well. The proof
  capability family (i) does **not** — money, quantity, conditional/event-input narrowing, and the
  witness gate all extend the same discharge code (`BuildNarrowedIntervals` / `ProofEngine.cs`), so
  under the disjoint-file rule they collapse into a mostly-serial chain. So Layer 2 is parallel between
  the families and for the conformance half, but the hardest, highest-value half — the proof
  capabilities — largely serializes on the shared proof engine; do not plan throughput as if all of
  Layer 2 fans out.*

- **Layer 3 — Runtime-contract closure + final certification (serial).** Close denominator C (every
  handed-off fault named + dispositioned in the runtime-contract spec; the ~17/24 missing captures),
  then run the § 10 closure checks and the legibility sign-off.

**Sequencing within a layer** follows the ledger's declared dependencies (e.g. the witness value-rows
degrade gracefully until event-input narrowing lands — a soft dependency). The plan states, per unit,
what must precede it and what it may run beside.

### 7.3 Owner-legibility is a co-equal exit gate, not a nicety

The plan does not ship until the owner can read the whole thing in plain language and sign off.
Legibility and agent-executability are **the same property viewed twice**: plain language + defined
terms + clear structure make the plan simultaneously reviewable by the owner and correctly executable
by a context-free agent — coined jargon and label-soup propagate as implementation errors for the
agent exactly as they obscure review for the owner. Mechanically checkable: no undefined coined term;
no bare internal label carrying load-bearing meaning; a worked example for every behavioral claim; the
plan scans cleanly top to bottom. An independent context-free-legibility pass asks: *could an agent
with none of this conversation's context build exactly this unit from the brief alone?*

### 7.4 Engineering-diagnosis-first at hard slices (the anti-spiral rule)

When a slice hits a wall — a case the engine cannot prove, a reuse that over-proves, a "mostly-reuse"
scope that explodes on contact — the **first artifact is a one-page engineering diagnosis of the
boundary**, not a re-opening of "what should Precept be." The diagnosis answers three questions: what
is *genuinely undecidable*, what is *merely-unimplemented* (buildable, just not built), and what *one
authored rule* would discharge it (the § 6 escape valve). A design or product-identity re-litigation
is permitted **only if that diagnosis fails to close.** This is the countermeasure to the failure that
cost roughly a month in the June→July arc (Frank's 2026-07-13 retrospective): the June "2c-ii"
amendment already *held* the correct engineering diagnosis — the over-prove on unbounded references
and the missing default-fold — but the team set it down and picked up the philosophy question instead.
The reflex will get more chances to fire (the aggregates design, any §1b revisit, the overflow work);
this rule fires first, every time.

---

## 8. Verification discipline and model/independence strategy

**Source-grounded verification is non-negotiable.** Every gap/inconsistency finding = source at
file:line (against committed HEAD) + a live `precept_compile` probe, quoted verbatim. No
`passed / closed / green` claim without literal evidence (commit hash, literal `Passed: N`, verbatim
diagnostic output). Absence of tool output is UNKNOWN, never success.

**Rebuild the MCP first** (§ 3.0); after any `src/Precept` change, `/mcp reconnect precept` before
re-probing. An MCP-vs-green-test disagreement means suspect a stale server before a code bug.

**Guard against self-validating spec edits.** When this session edited the spec, spec-first checks grep
**committed HEAD**, not the working tree.

**Model / independence tiering (cost-disciplined):**

| Work | Tier | Why |
|---|---|---|
| Gap analysis — the heavy spec-vs-source-vs-probe reasoning across all five angles | **Opus, extensive** | Must hold the whole spec and reason across it; denominator A's completeness rests on it. Not Fable (cost), not cheap, not document-only. |
| Independence-critical passes — re-extraction diff, behavioral false-Prove hunt, tail critic, adversarial diff review, legibility review | **Fresh independent Fable agent — never a fork** | Independence is the point; a fork *is* the main loop and only launders the orchestrator's framing back as reviewed. Ground the fresh agent at the source docs through its prompt. |
| Mechanical — doc drift-fixes, citation sweeps, register extraction, drafting a matrix from a clear spec | **Haiku / Sonnet** | Cheap; no independence or deep synthesis. |
| The mechanical build — worktree coding agents closing a unit's RED suite | **coding-agent tier, no Fable** | The RED suite + reuse pointers make it well-specified execution; the diff gets a separate Fable adversarial pass. |
| Hard synthesis where this session's context IS the asset and independence is not needed | **Opus or a fork** | A fork inherits full context; correct only when continuing this session's own reasoning. |

**Owner-consultation gate throughout.** Distinguish *implement-against-locked-spec* (the majority —
proceed) from *new language surface* (keyword/type/operator/modifier/construct/syntax, or a genuine
spec contradiction — Tier 2/3 conversation first, prior locked decision quoted verbatim, then wait).
The ledger's disposition field records this call per row so the boundary is post-hoc verifiable.

---

## 9. Where this diverges from the v3 plan, and how the v3 slices fold in

The v3 plan is good work at the wrong scope; the divergences are about scope and sequencing, not the
quality of its rulings.

1. **Scope: whole compiler, not an MVP subset.** v3 narrowed readiness to a proof-engine MVP (7
   capability slices) and left the completeness backbone un-owned — the ~236 open spec-conformance
   rows, the creation-completeness axis, and the runtime-contract closure all fall outside it. This
   meta-plan re-widens to the three-denominator whole. *Why: the north star is a complete, sound,
   spec-conformant compiler; an MVP subset cannot be certified "complete" against the spec denominator.*

2. **Prevention split into two axes; v3 covers only one.** v3's "fail-open holes + sweep" is
   discharge-soundness. Creation-completeness has no checker and no owner in v3. This meta-plan adds the
   creation spine as **three** mechanical checks — the walk refactor (child positions *and* roots,
   guards included) + a field-derived reflection checker over the whole reachable-record graph + a
   catalog-attachment check — not one. *Why: a missing obligation is a false-Prove invisible to a
   discharge audit; and it can go missing three independent ways (an unwalked position, a short
   position catalog, an unattached requirement), so it takes three checks to close it, all mechanical.*

3. **BUG-031/032 refiled from discharge to creation.** v3 folds them into its discharge sweep. Verified
   against HEAD, they are creation-axis defects (guard-condition entry point and member-argument child
   position never walked). This meta-plan makes them the walk-refactor's first failing tests. *Why:
   auditing discharge paths will never surface a position that has no discharge path.*

4. **The discharge sweep is no longer presented as a proof of soundness.** v3's sweep is retained but
   named shape-limited, paired with positive creation probes and a standing behavioral false-Prove
   hunt, and the arithmetic-false-Prove class is a *stated residual* (§ 4.4, § 10). *Why: an interval
   sign error returning a wrong non-null "proved" passes every static check; certifying the sweep as
   "soundness closed" is an overclaim.*

5. **Substrate-before-capability re-sequencing.** v3's Slice 0 lays the verdict/certificate substrate
   but not the creation guard; capabilities then build on an unguarded base. This meta-plan puts both
   substrate pieces in Layer 0 *before* any capability, so every capability inherits the guard. *Why:
   building capabilities first and the checker later forces re-auditing every capability — rework by
   construction.*

6. **The v3 slices are kept, not discarded.** Money / case-by-case / witness / single-fact /
   constant-rule / structural-severity all survive as Layer-2 capability units, with their rulings and
   reuse pointers intact. This meta-plan changes *what layer they sit in and what guards them*, not
   their internal design. *Why: the v3 rulings are sound; the defect was the missing backbone around
   them.*

7. **Completeness is a chain of counts, not a Definition-of-Done narrative.** v3's DoD is a checklist
   of capabilities green. This meta-plan's DoD is the § 10 closures. *Why: "all six capabilities green"
   cannot answer "did we cover the spec / every fault position / the runtime handoff" — the questions
   that make the plan *complete* need denominators, not a capability checklist.*

**A fair reading of v3:** if the owner's genuine intent is "ship the proof MVP first, defer the rest,"
v3 is coherent *for that subset*, and this meta-plan's stages can be scoped to the proof-relevant
ledger rows as a first increment. But the brief's north star is the whole compiler, and this
methodology sizes to that.

---

## 10. How "the readiness plan is complete" is proven

The plan is certified complete — ready to hand fresh coding agents, then hand off to runtime
development — when **all** of the following hold. Each is a count or a mechanical gate; each states
exactly what it proves and no more.

1. **Denominator A closed (mechanically for four angles; strongly-backstopped for the spec-claim
   angle).** Each of the five angles' counts matches its source; for the spec-claim angle, the
   re-extraction diff is zero, its register equals the modal-verb lint's hit-set, and the structural
   coverage floor is green (every normative section, table, and worked-example block yields ≥1 claim or
   an explicit "no normative content" marking); every entry has exactly one disposition (`denominator −
   dispositioned = 0`); every non-conformant entry is probe-backed; the v1 backstop leaves no
   unexplained prior row. *(The four source-countable angles close by subtraction. The spec-claim angle
   rests on an **agreement-plus-lint-plus-structural-floor backstop, not a totality proof** — § 5.2 —
   with a named residual: a claim both buried below block granularity and missed by both readers.)* *(v1
   reconciliation is a regression-catcher against v1's incomplete coverage, not a totality proof — the
   five-angle union + the re-extraction gate carry totality. And the v1 join itself is a **manual
   best-effort match** — v1's prose bullets carry no stable IDs, so matching them to the fresh ledger's
   section-anchor + quoted-claim keys is judgment, not a mechanical count; it is scored as backstop
   coverage, not a clean subtraction.)*

2. **Denominator B — creation axis closed (mechanically, as a conjunction of three checks).** Creation
   completeness is **walk-reachability ∧ carrier-enumeration ∧ attachment-completeness** (§ 4.3), all
   green, plus the no-dead-code guard: (i) the walk iterates the declared child positions *and* the
   declared root set (guards included), so declared = walked by construction; (ii) the field-derived
   reflection checker is green over the **whole reachable-record graph** — every `TypedExpression` /
   `TypedExpression?`-typed field on any record reachable from an obligation root (expression subtypes,
   guard/root carriers, wrapper carriers) is a declared, walked position or an N/A-with-reason; (iii)
   every faultable catalog operation declares its `ProofRequirements` and every hand-coded `Empty`
   attachment site is justified. What this buys, stated exactly: a new expression subtype, a new
   guard/wrapper/entity-carrier field, *or* a new faultable catalog member cannot escape without a red
   build **because all three checks are wired** — the guarantee is the conjunction of the three, no
   stronger. It does not extend to anything none of the three measures (e.g. a discharge-arithmetic
   over-prove — item 3).

3. **Denominator B — discharge axis closed *as far as it can be*, with a named residual.** The
   fail-open sweep is complete (every `bool?` routes null→reject; every dict-write guards ⊥); every
   covered position has a positive creation probe; the property-based / metamorphic arithmetic pass and
   the standing behavioral false-Prove hunt have both run to their time-box and every false-Prove they
   found is fixed. **Stated residual (not certified closed):** an arithmetic bug returning a wrong
   non-null "proved" is outside the sweep's shape-denominator; its mitigation is the property-based pass
   (invariants asserted, not proved) plus the standing behavioral hunt, re-run whenever discharge
   arithmetic changes. This is named as an open hole, not marked N/A — and the reason it stays open is
   the missing discharge oracle (§ 4.4), surfaced to the owner in § 11.

4. **Denominator C closed.** Every fault the compiler hands off is named and dispositioned to exactly
   one side in the runtime-contract spec; no dangling capture; the compiler/runtime boundary reads
   consistently. (Runtime code stays spec-only by design; this is a closed *contract*, not running
   code.)

5. **No orphans, both directions.** Every gap-claim → criterion → test → work-unit; every criterion /
   test / behavioral change → a claim. Countable, on section-anchor + quoted-claim keys.

6. **Every work unit is agent-brief-shaped.** RED suite, stable spec refs, file:line reuse pointers,
   disjoint scope boundary, doc-touch obligations. Checkable per unit; the unit set partitions the gap
   list (every gap in exactly one unit; union = the whole; dependency graph acyclic with a marked
   parallel frontier).

7. **The corpus, the reflection checker, and all analyzers are green** — full-compile, no regression.
   Two distinct budgets, not one: the **corpus compiles within the runtime compile-time budget** (the
   whole corpus in ~44ms, worst single file ~3ms — a per-compile clock), while the new reflection /
   presence analyzers cost **`dotnet build` time** (a build-time clock, paid once per build, not per
   compile). Each is held to its own budget; a build-time analyzer must not be scored against the
   compile-time number or vice versa.

8. **The owner has signed the legibility pass** — no undefined coined term, no bare load-bearing label,
   a worked example per behavioral claim, scans cleanly.

When 1–8 hold, "the readiness plan is complete" is not something anyone must believe — it is what the
checks have shown, re-runnable by the owner or a reviewer. The one place the plan does **not** claim a
mechanical proof — the discharge-arithmetic false-Prove residual (item 3) — is stated openly as a
residual, not certified away. That honesty is itself part of the guarantee: a prevention engine that
overclaims its own soundness is exactly the failure the product exists to prevent.

---

## 11. Honest limits and owner decision points (surfaced, not resolved)

**Honest limits (what this methodology does NOT guarantee):**

- **Runtime code.** The runtime is spec-only by design. "Compiler complete" = compiler-code
  conformance + a complete, self-consistent runtime-*contract* spec (denominator C). This plan does not
  build the evaluator.
- **Spec correctness beyond consistency.** Stage 0 guarantees the spec is internally consistent and
  scope-honest; it does not guarantee its design decisions are *right*. New-surface questions and
  locked-decision overrides route to the owner (the pre-design gate).
- **The discharge-arithmetic false-Prove residual (§ 4.4, § 10 item 3).** Mitigated by a property-based
  / metamorphic arithmetic pass and a standing behavioral hunt, **not** closed by a static proof. It
  stays open because there is no executable discharge oracle to test verdicts against — a build-order
  fact, not a design choice. Stated, not hidden. This means the methodology **certifies soundness
  modulo a searched residual, not soundness outright**: it structurally cannot fully certify the
  prevention guarantee, only shrink the gap and name what remains.
- **The denominator is only as complete as its five angles + the v1 backstop.** A miss must evade all
  five angles, the re-extraction gate, *and* the prior audit — unlikely, not impossible. The residual
  is stated.

**Owner decision points this methodology surfaces (and does not settle):**

- **Scope calls (Stage 0).** Which specced-but-unbuilt features are *will-not-build* (remove the claim)
  vs *intended target* (keep, honestly marked). Owner decision; not new-surface design.
- **Boundary rulings (Stage 0).** Any fault/constraint class whose side (compile-time / runtime-ingress
  / outside-envelope) is genuinely unsettled.
- **Contradiction forks (Stage 0).** Where impl contradicts spec and git history does not settle it as
  drift, the owner rules which is right.
- **The freeze sign-off.** The owner signs the frozen-spec diff; the whole chain derives from that
  commit.
- **`needs-design` items.** Genuinely new surface routed to a Tier-2/3 conversation + `/design` before
  entering the implement track.
- **The scope question itself.** Whole compiler (this plan) vs. keep the v3 MVP subset as a first
  increment (§ 9). This meta-plan sizes to the whole; the owner may rescope.
- **Is soundness-modulo-a-searched-residual an acceptable handoff bar? (§ 4.4, § 10 item 3.)** Creation
  completeness closes mechanically; discharge soundness does not, and cannot until a discharge oracle
  exists to test against. The property-based pass and behavioral hunt shrink the residual but do not
  eliminate it, and whatever they miss is inherited downstream. The owner decides whether "soundness
  down to a searched residual" clears the bar for declaring the prevention guarantee ready to hand off,
  or whether more is required first. The methodology surfaces this; it does not settle it.

This document is methodology. It produces no gap list and no code, and it settles none of the above —
it defines how they get answered with proof rather than judgment.

---

## Appendix — grounded facts this design rests on (verified against committed HEAD this session)

- `CollectObligations` (`ProofEngine.cs:208`) enrolls `.Condition` (rules/ensures, `:239/:247`),
  `ComputedExpression` (`:256`), and action inputs (`:491/:493`) as walk entry points; it does **not**
  enroll `.Guard` (BUG-031, creation-axis).
- `WalkExpression` (`ProofEngine.cs:275`) is a hand-written `switch (expr)` (`:282`) over
  `TypedExpression` subtypes with per-arm hand-picked child recursion; the `TypedMemberAccess` arm
  (`:319`) recurses `ma.Object` (`:322`) but not `ma.Arguments` (BUG-032, creation-axis). **No
  `default:` arm** — a new subtype silently escapes the walk.
- `TypedMemberAccess` (`SemanticIndex.cs:106`) carries `Object` (a `TypedExpression` field) and
  `Arguments` (an `ImmutableArray<TypedExpression>` field) as record fields — so a field-enumerating
  reflection/Roslyn pass mechanically flags `Arguments` as an unwalked position with no human in the
  loop. This is the case a subtype-scoped denominator *does* catch.
- **Entry-point carriers are not expression subtypes (why § 4.2's denominator must span the whole
  reachable-record graph, not just expression subtypes).** The walk's roots are enrolled by the
  hand-written list in `CollectObligations` (`ProofEngine.cs:208–270`); the guard condition `.Guard` is
  a `TypedExpression? Guard` field on `TypedTransitionRow` (`SemanticIndex.cs:526`), `TypedRule`
  (`:571`), `TypedEnsure` (`:582`), `TypedAccessMode` (`:592`), `TypedStateHook` (`:600`), and
  `TypedEventRow` (`:612`) — **none of which is a `TypedExpression` subtype.** Reflection scoped to
  expression subtypes cannot enumerate `.Guard`; BUG-031's class therefore sits *outside* a
  subtype-scoped denominator, and closing it requires the reflection pass to range over the guard/root
  carrier records too.
- **Wrapper carriers hold an expression but are not expressions.** `TypedHoleSegment`
  (`SemanticIndex.cs:158`) extends `TypedInterpolationSegment`, not `TypedExpression`, and carries a
  `TypedExpression Expression`; `TypedInterpolationSlot` (`:193`) is a plain record with the same
  `Expression` field. Expressions reachable only through these carriers are outside a subtype-scoped
  denominator — a second reason § 4.2 ranges over the reachable-record graph.
- **Obligation *attachment* is upstream of the walk (why walk-reachability alone cannot certify
  creation).** The walk only *reads* a node's pre-built `.ProofRequirements`; attachment happens in the
  type-checker before the walk runs — the operator path reads `resolvedOperation.ProofRequirements`
  from the catalog (`TypeChecker.Expressions.cs:683`), and other sites hand-code
  `ProofRequirements: ImmutableArray<ProofRequirement>.Empty` (`TypeChecker.Expressions.Callables.cs:364,
  374`). The two verified `Empty` sites are the malformed-action and unknown-action arms — error paths
  that already emit `TypeMismatch` and produce an error-typed node, so they are legitimate
  N/A-with-reason. A faultable operation whose catalog entry omits its requirement, or a hand-coded
  `Empty` where one is owed, yields a node the walk visits and finds empty — a false-Prove no
  walk-reachability check can see. This is why § 4.3 adds the catalog-attachment check as a distinct
  third leg.
- `ChildExpressionPositions` — **zero** code hits: no position catalog and no completeness checker
  exists.
- Existing analyzers `Precept0001`–`Precept0029` are catalog-shape / cross-ref / emission-coverage
  checkers: `Precept0007` GetMeta exhaustiveness, `Precept0019` marker-attribute *presence*, `Precept0027`
  diagnostic-emission coverage, `Precept0028` call-shape *presence* + allow-list. **None** analyzes
  which child positions a hand-written switch recurses into — confirming § 4.3's judgment that the
  sound form is a walk *refactor* (catalog-driven iteration + presence check), not an additive
  body-scanning analyzer.
- The coverage tests (`ExpressionFormCoverageTests`, `TypeFamilyCoverageTests`, `ParserCoverageGapTests`)
  validate catalog metadata **shape**, not spec **behavior**.
- Spec Status line claims "§5 Proof Engine complete"; live soundness holes make that an overclaim — a
  Stage 0 scope-honesty correction.
- The v1 audit (`compiler-readiness-plan-2026-06-11-appendices/spec-coverage-audit.md`) is 619
  prose-bullet rows, no stable IDs, 6-bucket classification (fully 383 / partial 85 / stub 20 /
  missing 53 / diverges 78), with a `## Canon inconsistencies reported` section — the regression
  backstop for § 5.5.
