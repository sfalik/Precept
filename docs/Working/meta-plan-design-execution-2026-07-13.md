---
title: "Meta-Plan — Methodology for a Complete-Compiler Readiness Plan (execution/TDD lens)"
status: Draft — owner-gated
date: 2026-07-13
author: Synthesis (Claude, execution lens), grounded against committed HEAD
owner: Shane
purpose: >
  The methodology for running the readiness pipeline (spec completeness+consistency → gap list →
  acceptance criteria → TDD tests → code) so that "the readiness plan is complete" becomes a
  checkable claim, not a judgment. This is METHODOLOGY — it does not itself produce the gap list
  or the code. It arms an owner decision; it settles nothing.
relationship_to_v3: >
  Widens the 2026-07-12 v3 plan from a proof-engine MVP subset to the whole compiler. The v3 slices
  are kept, refiled as a subset of one layer, and re-sequenced onto a substrate they currently lack.
  Divergences are stated explicitly in § 9.
---

# Meta-Plan — Methodology for a Complete-Compiler Readiness Plan

## What this document is (and is not)

**Is.** The recipe for running the readiness work so that each stage's completeness is *mechanically
verifiable* before the next stage consumes it, and so the artifact that comes out — the readiness
plan — decomposes into work units a **fresh, context-free coding agent** can build correctly and the
**owner** can read and sign off in plain language.

**Is not.** It is not the gap list, not the acceptance criteria, not the code, and not a second
proof-engine design. It produces none of those; it produces the *discipline and gates* under which
they get produced.

**The one-sentence spine.** Turn "is the readiness plan complete?" from a human judgment into three
mechanical closure checks — one per denominator (the whole spec, the prevention guarantee, the
runtime handoff) — where the highest-leverage check, prevention-completeness, is enforced by a
**build-time checker** so that "no fault position was left un-obligated" is answered by `dotnet
build`, not by an auditor's confidence.

---

## 1. The core reframing — one pipeline, but three denominators

The owner's stated pipeline is right:

> spec completeness+consistency → complete gap list → comprehensive acceptance criteria → TDD tests → code

The load-bearing correction this meta-plan makes is that **"the gap list" is not one list against one
denominator.** The north-star outcome has three structurally different completeness questions, each
with its own denominator (the set you must cover), its own way of proving coverage, and its own
verification technique. Collapsing them into one flat gap list is exactly how the v3 plan lost the
completeness backbone: it counted proof *capabilities* and never counted fault *positions* or spec
*clauses*.

| Denominator | The set to cover ("everything") | "Complete" means | Proven by | Primary verification |
|---|---|---|---|---|
| **A. Spec-conformance** | Every normative clause in the corrected canonical spec (every "shall / is / must / rejects") | Every clause maps to a passing conformance test or an owned gap-unit | Clause-set MINUS covered-set = ∅ | source read at file:line + live `precept_compile` probe |
| **B. Prevention-completeness** (the heart) | Two orthogonal axes — see § 4 | (i) every faultable position carries an obligation; (ii) no discharge path false-Proves | (i) a **build-time checker** goes green; (ii) the fail-open sweep closes | build-time checker + `Compiler.Compile` regression suite |
| **C. Runtime-contract-completeness** | Every fault the compiler hands off + every runtime-contract obligation | Every handed-off fault is named in the runtime-contract spec and either proved-at-compile-time or explicitly runtime-enforced; no dangling capture | Handoff-set MINUS contract-set = ∅ | spec cross-read + fault-catalog cross-read |

**Docs self-consistency (north-star item 4)** is not a fourth denominator — it is a *gate condition*
that rides on all three: a gap-unit does not close until its doc-touch obligations land in the same
commit (the project's existing doc-sync rule), and the spec-correction stage (§ 3) removes the
contradictions up front so the denominator itself is consistent.

Everything below is how each denominator's closure check is made real, and how the closed gap set
becomes failing tests and then code without the failure modes this project has actually hit.

---

## 2. The failure modes this methodology is built to prevent

These are not hypothetical. Each is a thing this project has done; each maps to a specific structural
move below, so the design can be checked against its purpose.

| Failure mode (observed) | Structural prevention (where) |
|---|---|
| **Un-owned work falls between slices** (the v3 narrowing left the completeness backbone with no owner; holes get found ad-hoc one probe at a time) | The three closure checks (§ 1) + the build-time position checker (§ 4) make "un-obligated position" a red build, not a missed audit. Every gap-unit has exactly one owner in the ledger (§ 5). |
| **Planning-loops with no code** (sessions circle re-deciding; no commits for weeks) | The gates are *artifacts that compile or a checker that runs*, not review meetings. Stage exit = a mechanical check passes, not "we agree it's done." The spec-correction stage is time-boxed and owner-gated; it cannot expand into a redesign (§ 3). |
| **Rework where slices re-touch the same files** | Layer-2 units are partitioned by **disjoint file ownership** (§ 7); two concurrent worktree agents never edit the same file. Genuinely-shared files force the gaps into one serial unit. |
| **False-green tests that skip the proof engine** (`Check` / `CheckExpectingClean` bypass the proof stage) | Acceptance criteria are `Compiler.Compile(...)` / `CompileExpectingError` only, enforced as a written rule per unit and checkable by grep at review (§ 6). |
| **Enumeration covers only the instance tripped over** (design-time family under-enumeration) | Each gap-unit expands to the **whole input family** with every cell disposed, and every `[StaticallyPreventable]` code it touches is verified live-not-dead (§ 6). |
| **Redefining the bar to pass** (an unprovable case reframed as an "acceptable conservative boundary") | Prevention denominator B is closed by a checker, not a narrative; an unprovable faultable position is a red build. "Unprovable = reject" is the invariant, mechanically (§ 4). |

---

## 3. Stage 0 — Spec completeness + consistency (the denominator is fixed first)

**Why first.** The corrected spec is the denominator for denominator A and the reference for B and C.
Deriving a gap list from a spec that still contradicts itself or still claims-implemented what the
code doesn't do propagates the drift into the tests and then the code. Fix the ruler before measuring.

**What Stage 0 does — three passes, in order:**

1. **Scope-honesty pass.** The spec states the *intended end-state* (features built or not). The axis
   is intended-vs-not and status-honesty, **not** built-vs-not. So: *remove* only features Precept
   will not build; *mark* status honestly where a doc claims "Implemented" but code disagrees (e.g.
   the spec Status line "§5 Proof Engine complete" is an overclaim against the live soundness holes —
   correct it to the honest state); never *falsely claim* a target is implemented; never annotate a
   real intended-but-unbuilt target as absent. Concrete inbound examples to resolve: `collection-types.md`
   is "Canonical design, not yet implemented" (keep as intended, mark honestly); §0.6 items 7–10 are
   marked "Specification-only" yet the v3 structural-severity work says item 7 now rejects (reconcile
   the drift).

2. **Internal-consistency pass.** Resolve the spec's own contradictions. The v1 audit already
   collected a `## Canon inconsistencies reported` section (e.g. `sqrt` integer-arg: §3.2 widening
   "in any context" vs §3.7 "integer input is a type error"; choice field-vs-field: `primitive-types.md`
   "always an error" vs spec §3.6 "allowed under same-element-type"). Each contradiction is either a
   **mechanical drift fix** (proceed — one side is plainly stale) or a **genuine fork** (two defensible
   readings). Forks route to the owner conversation gate; they are *not* settled inside this pass.

3. **Compiler/runtime boundary-consistency pass.** Verify §0.7 (the guarantee contract) and the
   runtime-contract docs draw the same line: every fault class the compiler declares
   `[StaticallyPreventable]` is prevented at compile time; everything else is explicitly a
   runtime-governance obligation. Inconsistencies here feed denominator C.

**Owner-consultation gate inside Stage 0.** Most Stage-0 edits are mechanical drift fixes (Tier 1 —
proceed). A contradiction that is a genuine fork, or any edit that touches *new* language surface, is
Tier 2/3 — surface it in plain text with the prior locked decision quoted verbatim, and wait. The
spec-correction stage **cannot** introduce or settle new surface unilaterally; that is the exact
conflation the consultation gate exists to prevent.

**Stage-0 completeness gate (mechanical).** Stage 0 is done when:
- Every doc Status field matches code for "Implemented" claims (verified, not asserted), and every
  intended-but-unbuilt feature is honestly marked, not removed.
- The `## Canon inconsistencies` set is fully dispositioned: each is fixed, or is an open owner-fork
  with a recorded neutral two-sided framing.
- The §0.7 / runtime-contract boundary reads consistently for every `[StaticallyPreventable]` fault.

**Time-box.** Stage 0 is bounded (it is drift-correction, not redesign). If it starts generating new
design, that is a signal to stop and route the specific item to `/design`, not to keep the stage open.

---

## 4. The load-bearing move — prevention-completeness as a compiled invariant

This is the spine of the whole meta-plan and the single sharpest divergence from v3. Read it as the
answer to "how does the methodology *produce* completeness without an endless manual audit."

### 4.1 Two axes the v3 plan conflates

The prevention guarantee (north-star item 2) has **two orthogonal completeness axes**, and they fail
independently:

- **Discharge soundness** — *given* an obligation was created at a fault position, does its discharge
  path ever return "proved" on an input it has not actually proved? A false-Prove here is a live
  runtime fault on a clean compile. **v3 covers this** via the four named fail-open holes + the
  systematic `bool?`-and-dict-write sweep.

- **Creation completeness** — is there an obligation *created at all* at every faultable position? A
  missing obligation is *also* a false-Prove (the position compiles clean because nothing was ever
  checked), but it is invisible to the discharge sweep — there is no discharge path to audit. **v3
  does NOT cover this.** Verified against HEAD: `CollectObligations` (`ProofEngine.cs:208`) walks
  `.Condition` for rules/ensures/computed fields but never enrolls `.Guard` (BUG-031 — every faulting
  arithmetic in a `when` guard collects zero obligation → false Proved); `WalkExpression` is a
  hand-written switch whose `TypedMemberAccess` arm (`:319`) recurses into `ma.Object` but not
  `ma.Arguments` (BUG-032). Neither is a discharge bug — the obligation is simply never born. And
  `ChildExpressionPositions` has **zero code hits**: no checker enforces that the walk covers every
  child position.

**v3 files BUG-031/032 into its Slice-0 discharge sweep. That is a mis-file** — they are creation-axis
defects, and auditing discharge paths will never surface a position that has no discharge path. This
is precisely how "un-owned work falls between slices."

### 4.2 The checker: make "every faultable position is obligated" a build error

The single highest-leverage move in this meta-plan is to convert creation-completeness from a manual
audit into a **build-time invariant**, exactly as the catalog system already converts "every catalog
member is handled" into `Precept0007` build errors.

**The checker (a new Roslyn analyzer, sibling to Precept0001–0029), backed by a new catalog axis:**

- **Catalog axis — `ChildExpressionPositions`.** For every `TypedExpression` subtype, declare (in
  catalog metadata, catalog-before-code) the complete set of its child-expression-bearing positions
  and, for each, whether that position is *fault-reachable* (can host a faulting arithmetic / access /
  bounded-write subexpression). This is the machine-readable statement of "what the walk must reach."

- **Layer 1 — walk-coverage.** The analyzer proves, at build time, that for every `TypedExpression`
  subtype, `{declared fault-reachable child positions}` MINUS `{positions the `WalkExpression` arm
  actually recurses into}` = ∅. A new subtype, or a new child position on an existing subtype, that
  the walk does not reach is a **red build** — the `ma.Arguments` omission (BUG-032) becomes
  impossible to reintroduce.

- **Layer 2 — construct-entry coverage.** The analyzer proves every obligation-bearing construct
  position (`.Condition`, `.Guard`, computed `.ComputedExpression`, action input expressions, …) is
  enrolled as a walk entry point in `CollectObligations`. The `.Guard` omission (BUG-031) becomes a
  red build.

- **Layer 3 — no dead preventable code.** The analyzer proves every `[StaticallyPreventable]` fault
  code has at least one *live obligation-creation* site (not merely an emission site — Precept0027
  already checks emission). A `[StaticallyPreventable]` fault with no creation path is a preventable
  fault that is never prevented — a P11 hole — and is a red build. (The v1 audit found several such:
  `FunctionArgConstraintViolation` PRE0022, `ChoiceElementTypeMismatch` PRE0088, `ChoiceMissingElementType`
  PRE0090 all had zero emission — this layer makes the class detectable, not just the instances.)

**Why this is the spine.** Once the checker is live, prevention-completeness (axis B-i) is *closed by
`dotnet build`*. Every capability slice built afterward **inherits the guard for free** — an agent that
adds a new proof strategy but forgets a child position gets a red build, not a silent hole discovered
three months later. It converts the project's actual recurring failure (holes found one probe at a
time, work falling between slices) into a compiled invariant. BUG-031 and BUG-032 stop being
"soundness holes to remember" and become **the checker's first two failing tests**.

**Substrate-before-capability sequencing (the anti-rework ordering).** The checker must be built
*before* any capability slice, for two reasons: (1) it is the completeness gate those slices are
measured against; (2) it guards them as they land. Building capabilities first and the checker later
means re-auditing every capability against the checker after the fact — rework by construction.

---

## 5. The gap ledger — stable identity, spec-keyed, reconciled against v1

The "complete gap list" stage produces one artifact: a **gap ledger**. Its completeness must itself be
provable, and it must survive being consumed by context-free agents.

### 5.1 Row shape (every field is load-bearing for a downstream agent)

Each row carries everything a fresh coding agent and the owner need:

- **Spec-clause anchor** — the exact normative sentence and stable citation (e.g.
  `precept-language-spec.md §3.3, "No implicit fallback… emits a diagnostic and assigns ErrorType"`).
  This is the join key to denominator A.
- **Source evidence** — the file:line where the code does (or fails to do) the thing, read this
  session against **committed HEAD**.
- **Live probe** — a `precept_compile` result captured against a freshly-rebuilt MCP (see § 8), quoted
  verbatim (the diagnostic codes emitted, or their absence). Document-only rows are rejected — this
  session's brief proved twice that document-only analysis both over- and under-states.
- **Classification** — the v1 six-bucket scheme (fully / partially / stub / missing / diverges) plus,
  for divergences, the drift-vs-decision call (check git history — AI-co-authored departures are
  cleanup, not "decision required").
- **Denominator + disposition** — which of A/B/C; and one of: *implement-against-locked-spec* (proceed),
  *owner-fork* (new surface or genuine contradiction — conversation first), or *out-of-scope-with-trigger*
  (deferred, with its re-open condition, never a bare defer).
- **Owner** — the single work-unit that will close it. No row is owner-less; no row has two owners.

Rows are referred to **by plain description of the defect** ("exponent literals silently resolve to
zero"), never by an invented ID or label. Stable identity for *joining* is the (spec-clause anchor +
classification) tuple, mirroring how the v1 audit is joined — but that tuple lives in the row, it is
not a bare code shown to the reader.

### 5.2 Completeness gate for the ledger itself (proving nothing was missed)

Two mechanical closures, both required before acceptance criteria may start:

1. **Forward closure (spec is the denominator).** Walk the corrected spec clause by clause; the set of
   normative clauses MINUS the set of clauses that have either a gap-row or a passing conformance test
   = ∅. A clause with neither is an un-enumerated gap — the ledger is not yet complete.

2. **Backstop reconciliation (against the v1 audit).** The v1 audit's 619 rows are a month stale and
   pre-date this session's fixes, so they cannot be trusted forward — but they are a real prior
   enumeration and a miss-detector. Join each v1 non-`fully` row (by spec-clause anchor + claim +
   citation, since v1 has no stable IDs) against the fresh ledger. Every v1 row lands in exactly one
   bucket: **re-derived** (fresh ledger has it), **closed-since** (a probe shows it now passes —
   record the closing commit), or **superseded** (Stage-0 spec correction removed/changed the clause).
   A v1 row that is *none of these* is a **miss in the fresh derivation** — go find why. This is the
   backstop against under-derivation.

The gap-analysis stage that produces this ledger is the **heavy, Opus-extensive** pass (per the
constraints): source-grounded, probe-verified, run on the model tier that can hold the whole spec and
reason across it — not on a cheap model and not document-only.

---

## 6. Acceptance criteria → failing tests (the execution mechanics, TDD)

This is where the ledger becomes buildable. The rule is **test-first, family-complete, full-compile**.

### 6.1 One gap-row → one input-family matrix (not one instance)

The recurring project failure is enumerating only the instance tripped over. So each gap-row expands
to its **whole input family**, every cell explicitly disposed. The family axes for a prevention gap are
the cross product the project has learned to enumerate: `{numeric / length / count / qualifier}` ×
`{the fault-prone shapes that host it}` × `{the sources — literal / field-ref / arg / computed / guard
/ reject-row}`. For a spec-conformance gap it is the enumerated surface the clause governs (e.g. for
"choice default membership": in-domain / out-of-domain / wrong-element-type / empty-domain).

**Family-completeness gate:** every cell of the matrix has an explicit disposition — a failing test, a
passing test, or a recorded N/A with a reason. A matrix with an un-disposed cell is not done.

### 6.2 The tests are written RED first, as executable definition-of-done

- Each cell that should emit becomes a `# EXPECT:`-contracted diagnostic sample under
  `test/integrationtests/diagnostics/*.precept`, drift-guarded by `DiagnosticSampleDriftTests`, plus a
  full-compile assertion. Each cell that should discharge becomes a clean-compile assertion.
- **Full-compile only.** Every assertion drives the whole pipeline — `Compiler.Compile(...).Diagnostics`
  / `CompileExpectingError` — **never** the type-checker-only `Check` / `CheckExpectingClean`, which
  skip the proof engine and produce false greens on exactly the diagnostics these units touch. This is
  checkable by grep at review: a proof-touching unit whose tests call `Check` is rejected.
- **`[StaticallyPreventable]` liveness.** For every preventable fault code the unit touches, a test
  asserts it actually emits on the violating cell — closing the "dead preventable code" class the
  Layer-3 checker (§ 4.2) detects structurally. Delivered-vs-claimed stays honest: a code that cannot
  be made to emit is an open hole to flag, not a cell to quietly mark N/A.

### 6.3 The RED suite IS the agent brief's definition-of-done

The failing matrix is not a separate document — it is the unit of work the coding agent receives and
the thing it checks itself against. The agent is done when the RED suite is GREEN, the position checker
is still green, and the corpus (77 samples + `# EXPECT:` contracts) is still green. This is what makes
a unit *self-verifying*: the agent knows when it is done without the design conversation's context.

**Acceptance-criteria completeness gate (before any code):** for the whole ledger, every gap-row has a
family-complete RED matrix, every matrix is full-compile-based, and every touched preventable code has
a liveness test. Derived from the **corrected** canon (Stage 0), against **committed HEAD**.

---

## 7. Work-unit shape and dependency-ordered sequencing

The readiness plan the pipeline outputs is a set of **work units**. This section is the contract each
unit meets and the order they run — the part that makes the plan executable by fresh coding agents
without rework, and legible to the owner.

### 7.1 The self-contained agent brief (the artifact IS the prompt)

Each unit carries, in plain language:

- **What it does** — a plain-language description, used as its name. No `W-A` / `Slice-8` /
  `F-LANG-COLL-06` / `D-3` labels as structure. Coined terms defined on first use or written in plain
  prose (define-or-plain-prose). The owner can read the unit and know what it delivers.
- **Its RED suite** — the family-complete failing tests (§ 6), the definition-of-done.
- **Exact canonical spec references** — the clause anchors from its gap-rows (stable citations only:
  spec §, catalog file, stage doc — never a `docs/Working/` path or a finding ID).
- **Source-reuse pointers (file:line)** — the existing catalog entries and code seams to *reuse*, never
  fork. New code is a thin wiring layer over existing catalog/machinery; a parallel definition is a
  review failure. (The v3 §3-money unit is the model: "attach `IntervalTransfer` to the op-meta family
  via catalog wiring — zero engine code.")
- **Its scope boundary** — the disjoint file set it owns, and what it explicitly does not touch.
- **Doc-touch obligations** — the canonical docs it updates in the same commit (per the CLAUDE.md
  routing table).

**Agent-sized.** One unit = one coding agent, one bounded worktree run. A unit that cannot be
completed in one run is split. Oversized units are the second-largest source of agent failure after
missing context.

### 7.2 The dependency layers (substrate → soundness → capability+conformance → closure)

The ordering is chosen to eliminate rework — each layer is a stable base the next builds on, so nothing
gets re-touched:

- **Layer 0 — Substrate (serial; blocks everything).** (a) The verdict/certificate shape — the
  `ProofVerdict` three-way DU + certificate format + `CertificateSteps` catalog (v3's Slice 0, kept
  intact — genuinely foundational; every capability's tests assert on the verdict shape). (b) The
  `ChildExpressionPositions` catalog axis + the three-layer position-completeness checker (§ 4).
  BUG-031 and BUG-032 are the checker's first failing tests. *Rationale: every downstream unit either
  asserts on the verdict shape or must not reintroduce an un-obligated position; both guards must exist
  first.*

- **Layer 1 — Discharge-soundness correction (mostly serial; blocks capabilities that extend the
  narrowing base).** The four fail-open holes on *already-walked* obligations (arg/field-name collision,
  sequential staleness, undecidable-default-bound, dead-end stamping) + the `bool?`-and-dict-write
  sweep. *Rationale: the capability slices extend `BuildNarrowedIntervals`; correct the base before
  adding capability on top — v3's own "corrected Slice-0 base" insight, now correctly separated from
  the creation-axis work it was tangled with.*

- **Layer 2 — Capabilities + spec-conformance gaps (heavily parallel; partitioned by disjoint file
  ownership).** Two families running concurrently:
  - The v3 proof capabilities — bounded money/quantity arithmetic, conditional/event-input narrowing,
    the witness gate + family grid, multi-term single-fact, constant-rule-to-bounds. These are refiled
    here as capability units *on top of* the Layer-0 checker they previously lacked.
  - The non-proof spec-conformance gaps from denominator A — exponent literals resolving to zero,
    choice-default membership unchecked, temporal `date ± '30 days'` resolution, the dead diagnostic
    codes (PRE0022/0088/0090), CI-flag enforcement, and the rest of the ~236 open v1 rows re-derived
    fresh.
  *Rationale for parallelism: these touch **disjoint file sets** (lexer/temporal/choice gaps vs proof
  gaps live in different files), so worktree-isolated agents run concurrently with clean merges. Where
  two gaps genuinely share a file, they collapse into one serial unit rather than two — this is the
  anti-rework partition rule.*

- **Layer 3 — Runtime-contract closure + final certification (serial).** Close denominator C (every
  handed-off fault named + dispositioned in the runtime-contract spec; the ~17/24 missing captures),
  then run the three closure checks (§ 1) and the legibility sign-off (§ 7.3).

**Sequencing within a layer** follows the ledger's declared dependencies (e.g. the witness value-rows
degrade gracefully until event-input narrowing lands — a soft dependency, not a hard block). The
readiness plan states, per unit, what must precede it and what it may run beside.

### 7.3 Owner-legibility is a co-equal exit gate, not a nicety

The readiness plan does not ship until the owner can read the whole thing in plain language and sign
off. Legibility and agent-executability are **complementary**: plain language + defined terms + clear
structure make the plan simultaneously reviewable by the owner and correctly executable by a
context-free agent, because *the artifact is the prompt* — coined jargon and label-soup propagate as
implementation errors for the agent exactly as they obscure review for the owner. The legibility gate
is mechanical enough to check: no undefined coined term; no bare internal label carrying load-bearing
meaning; a worked example for every behavioral claim; the plan scans cleanly top to bottom.

---

## 8. Verification discipline and model/independence strategy

**Source-grounded verification is non-negotiable** (the hard constraint). Every gap/inconsistency
finding = source at file:line (against committed HEAD) + a live `precept_compile` probe, quoted
verbatim. No `passed / closed / green` claim without the literal evidence (commit hash, literal
`Passed: N`, verbatim diagnostic output). Absence of tool output is UNKNOWN, never success.

**Rebuild the MCP first.** The precept MCP serves its last-spawn build; after Stage 0 (which edits
docs, not code) it is fine, but before the gap-analysis probes and after any `src/Precept` change, ask
the owner to `/mcp reconnect precept` so probes reflect current HEAD. An MCP-vs-green-test disagreement
means suspect a stale server before suspecting a code bug.

**Guard against self-validating spec edits.** When this session has edited the spec, spec-first /
"already decided?" checks grep **committed HEAD**, not the working tree — or a disputed this-session
edit self-validates.

**Model / independence tiering (cost-disciplined):**

| Work | Tier | Why |
|---|---|---|
| Gap analysis; the heavy spec-vs-source-vs-probe reasoning | **Opus, extensive** | Must hold the whole spec and reason across it; the completeness of denominator A rests on it. Not Fable (cost), not a cheap model, not document-only. |
| Design authoring; design review; adversarial review of a diff | **Fresh, independent Fable subagent — never a fork** | Independence is the entire point; a fork *is* the main loop (same model, context, priors) and only launders the orchestrator's framing back as if reviewed. Ground the fresh agent through its prompt at the source docs, not the orchestrator's assumptions. |
| Mechanical work — doc drift-fixes, citation sweeps, applying a specified change, drafting a matrix from a clear spec | **Haiku / Sonnet** | Cheap; no independence or deep synthesis needed. |
| The mechanical build — worktree coding agents closing a unit's RED suite | **coding-agent tier, no Fable** | The RED suite + reuse pointers make it well-specified execution. Adversarial *diff* review is a separate Fable pass, outside the building agent. |
| Hard synthesis where this session's context IS the asset and independence is not needed | **Opus or a fork** | A fork inherits full context; correct only when continuing this session's own reasoning, never for anything needing an independent check. |

**Owner-consultation gate throughout.** Distinguish *implement-against-locked-spec* (the majority of
gap-closing — proceed) from *new language surface* (keyword/type/operator/modifier/construct/syntax, or
a genuine spec contradiction — Tier 2/3 conversation first, prior locked decision quoted verbatim,
then wait). The gap ledger's disposition field records this call per row so the boundary is
post-hoc-verifiable.

---

## 9. Where this diverges from the v3 plan, and why

Stated plainly so the owner can weigh each divergence. The v3 plan is good work at the wrong scope; the
divergences are about scope and sequencing, not about the quality of its rulings.

1. **Scope: whole compiler, not an MVP subset.** v3 narrowed readiness to a proof-engine MVP (7
   capability slices) and left the completeness backbone un-owned — the ~236 open spec-conformance rows,
   the creation-completeness axis, and the runtime-contract closure all fall outside it. This meta-plan
   re-widens to the three-denominator whole. *Why: the north star is a complete, sound, spec-conformant
   compiler; an MVP subset cannot be certified "complete" against the spec denominator.*

2. **Prevention split into two axes; v3 covers only one.** v3's "four fail-open holes + sweep" is
   discharge-soundness. Creation-completeness (an obligation exists at every faultable position) has no
   checker and no owner in v3. This meta-plan adds the `ChildExpressionPositions` axis + build-time
   checker as the spine. *Why: a missing obligation is a false-Prove invisible to a discharge audit;
   only a position checker closes it, and only mechanically.*

3. **BUG-031/032 refiled from discharge to creation.** v3 folds them into its Slice-0 discharge sweep.
   Verified against HEAD, they are creation-axis defects (guard-condition and member-argument positions
   never walked). This meta-plan makes them the position checker's first failing tests. *Why: auditing
   discharge paths will never surface a position that has no discharge path — the mis-file is how the
   hole would survive the sweep.*

4. **Substrate-before-capability re-sequencing.** v3's Slice 0 lays the verdict/certificate substrate
   but not the position checker; capabilities then build on an unguarded base. This meta-plan puts both
   substrate pieces in Layer 0 *before* any capability, so every capability inherits the completeness
   guard. *Why: building capabilities first and the checker later forces re-auditing every capability —
   rework by construction.*

5. **The v3 slices are kept, not discarded.** Money / case-by-case / witness / single-fact /
   constant-rule / structural-severity all survive as Layer-2 capability units, with their rulings and
   reuse pointers intact. This meta-plan changes *what layer they sit in and what guards them*, not
   their internal design. *Why: the v3 rulings are sound; the defect was the missing backbone around
   them, not the slices themselves.*

6. **Completeness is a closure check, not a Definition-of-Done narrative.** v3's DoD is a checklist of
   capabilities green. This meta-plan's DoD is three mechanical closures (§ 10). *Why: "all six
   capabilities green" cannot answer "did we cover the spec / every fault position / the runtime
   handoff" — those are the questions that make the plan *complete*, and they need denominators, not a
   capability checklist.*

---

## 10. How "the readiness plan is complete" is proven

The readiness plan is certified complete — ready to hand fresh coding agents and then hand off to
runtime development — when **all** of the following mechanical checks pass. None is a judgment call.

1. **Denominator A closed** — every normative spec clause maps to a passing conformance test or a
   closed work-unit; the v1-audit backstop reconciliation leaves no unexplained prior row.
2. **Denominator B closed** — the position-completeness checker is live and green (every faultable
   position obligated; no dead `[StaticallyPreventable]` code), and the discharge fail-open sweep is
   complete with every hole it turned up fixed. Both axes, mechanically.
3. **Denominator C closed** — every fault the compiler hands off is named and dispositioned in the
   runtime-contract spec; no dangling capture; the compiler/runtime boundary reads consistently.
4. **Every work unit is agent-brief-shaped** — RED suite, stable spec refs, file:line reuse pointers,
   disjoint scope boundary, doc-touch obligations. Checkable per unit.
5. **The corpus and the position checker are green** — full-compile, in the performance budget.
6. **The owner has signed the legibility pass** — no undefined coined term, no bare load-bearing label,
   a worked example per behavioral claim, scans cleanly.

When 1–6 hold, "the readiness plan is complete" is not something anyone has to believe — it is
something the checks have shown. That is the whole point of the meta-plan.

---

## Appendix — grounded facts this design rests on (verified against HEAD this session)

- `CollectObligations` (`ProofEngine.cs:208`) enrolls `.Condition` (rules/ensures, `:239/:247`) and
  computed `.ComputedExpression` (`:256`) as walk entry points; it does **not** enroll `.Guard`
  (BUG-031, creation-axis).
- `WalkExpression` (`ProofEngine.cs:275`) is a hand-written switch over `TypedExpression` subtypes with
  per-arm hand-picked child recursion; the `TypedMemberAccess` arm (`:319`) recurses `ma.Object`
  (`:322`) but not `ma.Arguments` (BUG-032, creation-axis). No default/exhaustive arm.
- `ChildExpressionPositions` — **zero** code hits: no position-completeness checker exists.
- The build-time analyzers `Precept0001`–`Precept0029` are catalog-shape / cross-ref / emission-coverage
  checkers (e.g. `Precept0007` GetMeta exhaustiveness, `Precept0027` diagnostic-emission coverage). None
  checks obligation-position completeness or spec-behavior conformance.
- The coverage tests (`ExpressionFormCoverageTests`, `TypeFamilyCoverageTests`, `ParserCoverageGapTests`)
  validate catalog metadata **shape** (count, GetMeta completeness), not spec **behavior**.
- Spec Status line claims "§5 Proof Engine complete"; live soundness holes (BUG-031/032, and the v3
  fail-open holes) make that an overclaim — a Stage-0 scope-honesty correction.
- The v1 audit (`compiler-readiness-plan-2026-06-11-appendices/spec-coverage-audit.md`) is 619 prose-
  bullet rows, no stable IDs, 6-bucket classification (fully 383 / partially 85 / stub 20 / missing 53 /
  diverges 78), with a `## Canon inconsistencies reported` section — the reconciliation backstop for § 5.2.
