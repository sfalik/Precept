---
title: "Meta-Plan Design — Spec-Conformance & Traceability Spine"
status: Draft — owner-gated
date: 2026-07-13
author: Synthesis (Claude, conformance/traceability lens)
owner: Shane
purpose: >
  The METHODOLOGY (not the plan) for running the compiler-readiness pipeline —
  corrected spec → gap list → acceptance criteria → TDD tests → code — so that
  "the readiness plan is complete" becomes a CHECKABLE claim rather than a
  judgment. Its spine is spec-conformance traceability: every normative spec
  claim maps forward to a test and back, with a completeness gate at every stage
  that proves nothing was missed. This arms an owner decision; it settles nothing.
lens: SPEC-CONFORMANCE & TRACEABILITY
---

# Meta-Plan — Spec-Conformance & Traceability Spine

**What this is.** A methodology for running the whole compiler-readiness pipeline so
that its output — a readiness plan a fresh coding agent can build from — is *provably
complete against the spec*, not "complete because we looked hard." This document does not
produce the gap list or any code. It defines **how** to run the pipeline and **how each
stage proves it missed nothing** before the next stage consumes it.

**Central question (the lens).** How do we make *"implementation matches the corrected
spec"* provable end-to-end? Answer: give every normative spec claim a stable, legible
identity; carry it forward through a single chain — claim → acceptance criterion → test →
code — where **every arrow has a no-orphan gate in both directions**; and treat the spec as
the sole denominator, never a parallel checklist.

---

## 1. The one-paragraph shape

The spec is corrected and **frozen** as the denominator (Phase A). Every normative
sentence in it becomes a row in a **claim register** — a projection of the spec, keyed by
its own section anchor plus the quoted claim, not a new ID scheme (Phase B). Each claim
gets a **source-verified disposition** — conformant / gap / divergence / deferred — backed
by `file:line` and a live `precept_compile` probe, reconciled against the prior audit
(Phase C). In parallel, the prevention guarantee gets a second denominator the spec prose
alone does not give: a **position × fault matrix** — every syntactic position an expression
can occupy, crossed with every faultable operation — so "an obligation exists at every
faultable position" becomes a grid with no empty live cells (Phase C′). Every non-conformant
claim and every empty matrix cell becomes an **acceptance criterion**, then a **failing
test**, then a **self-contained work-unit brief** a context-free coding agent can build
(Phases D–E). Fresh worktree agents execute one unit at a time under the existing
`lifecycle-4-execute` rigor (Phase F). The readiness plan is **done** when every
compiler-side claim sits in the conformant-with-a-green-test column and every live matrix
cell has a creation test — a column count, not a judgment call.

---

## 2. The spine — bidirectional traceability against a single denominator

Everything below hangs off one artifact and one rule.

### 2.1 The claim register (the denominator)

The **claim register** is the enumeration of every normative statement in the corrected
spec surface — every "shall / must / is a compile-time error / rejects / is discharged /
is trusted." One statement, one row. It is the audit's denominator: completeness is
measured as *"what fraction of register rows sit in the conformant column,"* and that
fraction is only meaningful if the register is itself provably complete.

**Identity without label-soup (the move that makes traceability and legibility one thing).**
Each row's join key is **the spec's own section anchor plus the quoted claim** — e.g.
`spec §3.6 : "Collections are a type error inside string interpolation"`. Spec section
numbers are canonical and stable (CLAUDE.md treats them as citable-from-code); the quoted
sentence is inherently human-readable. There is **no coined `F-LANG-*` / `OQ1` / `D-3`
scheme** — those are exactly the transient labels the project bans, and the prior audit's
lack of stable IDs (prose bullets joined by "classification + claim + citation") is a
defect this fixes without replacing it with jargon. The register is a *projection of the
spec*, re-derivable from it — never a parallel document that can drift.

### 2.2 The no-orphan rule (the checkable core)

The chain is `spec claim → acceptance criterion → test → code`. Traceability means **no
orphans in either direction at any arrow**:

- **Forward:** every claim reaches ≥1 acceptance criterion (unless dispositioned
  conformant-already-tested or deferred-with-trigger); every criterion reaches ≥1 test;
  every test reaches code that turns it green.
- **Backward:** every acceptance criterion names the claim it serves (no criterion invents
  a requirement the spec does not state); every test names its criterion; every behavioral
  code change traces to a claim (a change with no claim is either undocumented spec surface —
  add the claim — or scope creep — remove it).

Orphans in either direction are the failure the whole methodology exists to make visible.
A forward orphan = a spec claim with no test = an unverified conformance assertion. A
backward orphan = a test/criterion/behavior with no spec basis = drift or invented scope.
**Both are gate failures**, and both are countable — which is the entire point.

---

## 3. The pipeline, with a completeness gate on every stage

Each stage is a transform; each **gate** is the proof that the transform lost nothing
before the next stage consumes it. Gates are *checkable*, not "reviewer felt good."

| Stage | Transform | Completeness gate (how "nothing missed" is proven) |
|---|---|---|
| **A. Spec correction** | Make the spec scope-honest, boundary-consistent, internally consistent; **freeze** it | Modal-verb lint over the frozen spec (every `shall/must/rejects/is a … error` sentence accounted for); zero unresolved self-contradictions in the reconciled inconsistency list; owner sign-off on the freeze commit |
| **B. Claim register** | Enumerate every normative sentence into register rows | **Independent re-extraction diff**: a second, fresh agent re-extracts the register from the frozen spec; the two extractions are diffed; every delta is resolved. The modal-verb lint's hit-set must equal the register's claim-set (no normative sentence without a row) |
| **C. Gap analysis** | Assign every claim a source-verified disposition | Every register row has a disposition + `file:line` + a live `precept_compile` probe transcript; **zero un-dispositioned rows**; every prior-audit non-conformant row maps to a register row or is explicitly retired (reconciliation backstop) |
| **C′. Position × fault matrix** | Enumerate faultable-position × fault cells; mark each covered / empty / N-A | Matrix derived from catalogs (positions × `Faults`/`ProofRequirements`), not hand-listed; **every live cell has an obligation-creation probe**; empty live cells become gap claims (§6) |
| **D. Acceptance criteria** | Derive criteria from gap-classified claims + empty cells | Bidirectional no-orphan check (§2.2): every gap-claim → ≥1 criterion; every criterion → a claim |
| **E. Work-unit packaging** | Bundle criteria into self-contained agent briefs | Every criterion lands in exactly one brief; every brief passes the context-free-legibility check (§8); dependency graph is acyclic and its edges are justified |
| **F. Execute** | Fresh worktree agents build unit-by-unit | Per-unit: failing tests exist first, go green; adversarial diff review; the register row flips to conformant only on a green full-compile test |

**Why A must come first and freeze.** The register, the criteria, the tests, and every build
agent are all *derived from the spec*. If the denominator moves under the audit, every
downstream artifact silently decouples from it. The spec today is **not** freezable as-is:
§0.6 embeds transient scaffolding (`F-LANG-SPEC-03/04/05` finding IDs, a "Phase 5" tracking
column, an "As of 2026-05-24" status table) inside normative prose, and the prior audit's
`## Canon inconsistencies reported` section lists live self-contradictions (e.g. §3.2's
"integer widens to number in any context" vs §3.7's "sqrt integer input is a type error").
A claim register built on a self-contradictory spec produces **unsatisfiable acceptance
criteria** — two tests that cannot both pass. So Phase A is a hard precondition, not a
warm-up.

---

## 4. Phase A in detail — the spec-correction precondition (the conformance gate)

Three sub-passes, then a freeze. This is the stage the current v3 plan under-serves: it does
per-slice doc reconciliation against a spec that still carries stale scaffolding and
unresolved contradictions.

**A1 — Scope-honesty.** Walk the spec feature-by-feature and classify each: *intended-and-
will-build* (keep as a normative target — the spec states end-state, so a not-yet-built
target stays stated, never annotated "absent"), or *will-not-build* (remove the claim — do
not leave dead surface as if intended). Strip embedded transient scaffolding from normative
sections: the `F-LANG-*` IDs, the "Phase 5" column, dated status tables move to the plan /
commit history, not the spec. **Gate interaction:** removing a *will-not-build* feature is a
scope decision the **owner** makes; it is not new-surface design. A feature that is
genuinely *new* surface (e.g. aggregates — already routed to `/design`, ledger #4) is marked
`needs-design` and pulled **out** of the conformance track into a separate design track — it
does not enter the claim register as an implementable claim until `/design` closes it.

**A2 — Compiler/runtime boundary consistency + runtime-contract closure.** Reconcile §0.7
(the compile-time/runtime guarantee contract) with §0.6 (proof obligations), and enumerate
the **runtime contract as a closed handoff**: for every fault class and every constraint
class, state which side owns it — *proven at compile time* (the compiler's carries-proof
obligation) vs *enforced at ingress by governance* (the runtime's operation-blind check) vs
*outside the envelope* (hydration / host-injected, trapped not re-governed). The spec today
has **zero** occurrences of "handoff / carries-proof / runtime contract" as enumerated
terms — the boundary lives only in §0.7 prose. This pass turns that prose into register
claims. **It requires no runtime code** — "runtime-contract complete" means the *spec's*
handoff enumeration is total (every fault/constraint class dispositioned to a side) and
self-consistent (no class claimed by both sides or neither).

**A3 — Internal consistency.** Resolve every entry in the reconciled inconsistency list
(seed: the audit's `## Canon inconsistencies reported`, line 875) so the frozen spec has no
claim that contradicts another. Each resolution is an owner-visible spec edit against
**committed HEAD** (never let a this-session working-tree edit self-validate).

**Freeze gate (A → B):** the corrected spec is committed and tagged as the conformance
denominator. Owner signs the diff. Nothing downstream is derived until this commit exists.

---

## 5. Phases B–C — building and dispositioning the denominator

**B — Register extraction.** Mechanical projection of the frozen spec into rows (cheap
model). Then the **independent re-extraction diff** gate: a *fresh, independent agent* (not
a fork — a fork launders the extractor's own reading) re-derives the register and diffs.
The register's claim-set must equal the modal-verb lint's hit-set. Deltas resolve to zero.

**C — Gap analysis (the heavy stage; Opus, extensive).** Per the hard constraint, rebuild
and reconnect the `precept` MCP first so probes reflect current HEAD. For each register row,
produce a disposition with **two kinds of evidence, both mandatory** (document-only analysis
is rejected — the prior sessions proved it over- and under-states):

1. **Source anchor** — `file:line` in `src/Precept/` (or the catalog) showing the behavior
   present or absent.
2. **Live probe** — a `precept_compile` transcript on a minimal `.precept` that exercises
   the claim, showing the actual diagnostic set.

Dispositions: **conformant** (impl matches, has a test) · **gap** (claim true, impl absent/
incomplete) · **divergence** (impl contradicts the claim — resolve which is right; often a
git-history check settles it as AI-co-authored drift, not a live decision) · **deferred**
(spec-only target with an explicit re-open trigger). **Reconciliation backstop:** every
non-`fully` row in the prior audit (`spec-coverage-audit.md`, 619 rows, 236 non-conformant)
must map to a register row's disposition or be explicitly retired with a reason. This catches
misses in either direction — a prior finding the new register dropped, or a new claim the
prior audit never reached.

**Gate C:** zero un-dispositioned register rows; every disposition carries both evidence
kinds; every prior-audit non-conformant row is mapped or retired.

---

## 6. The prevention sub-spine — obligation-position completeness (Phase C′)

This is the load-bearing addition the conformance lens forces, and the sharpest divergence
from v3. **Prevention is the product; its completeness has a denominator the spec prose does
not spell out.**

**The problem, grounded.** The spec (§0.7) says "at *every* fault-prone operation" the
compiler discharges a carries-proof obligation. But whether an obligation *exists* at a
given position is decided by `CollectObligations` (`ProofEngine.cs:208`), which walks a
**hardcoded** set of positions — transition/handler/hook actions, `Rules.Condition`,
`Ensures.Condition`, `ComputedExpression`, default expressions — and recurses via
`WalkExpression`. It does **not** walk guard (`when`) conditions. So faulting arithmetic in a
`when` guard is never visited, no obligation is created, and the write false-`Proves`
(BUG-031, confirmed). BUG-032 (member-call argument) is the same shape. There is **no
catalog** of faultable positions — `ChildExpressionPositions` has zero code hits. v3's
fail-open sweep audits *discharge* paths ("does `null` route to reject?") — it structurally
cannot catch a position that was never collected, because there is no obligation there to
discharge.

**The denominator.** Build a **position × fault matrix**, derived from catalogs, not hand-
listed:

- **Rows = every syntactic position an expression can occupy** — action RHS, computed-field
  expression, default expression, rule condition, ensure condition, **guard/`when`
  condition**, conditional arms, member-access object, call arguments, binary operands,
  interpolation holes, index expressions. This set becomes a new catalog
  (`ChildExpressionPositions`, or equivalent) so the enumeration is machine-owned and
  derivation-checked, exactly as the project requires of language surface — not a one-time
  audit list.
- **Columns = every faultable operation / fault class** — the `Faults` catalog (~15) and
  `ProofRequirements` (~13): divisor-nonzero, non-negative-input, bound-containment,
  presence, index-bounds, count-bounds, overflow (where in scope), etc.
- **Cells:** *covered* (an obligation is created here and a probe proves it), *empty-live*
  (this position can host this fault but no obligation is created — a soundness hole like
  BUG-031), or *N-A* (the fault cannot arise in this position, with a one-line reason).

**Every empty-live cell becomes a gap claim** feeding Phases D–F. **Every covered cell gets
an obligation-*creation* probe** (a `.precept` that puts the fault in that position and
asserts the obligation is raised) — distinct from v3's discharge-soundness tests. The
completeness gate is: *the matrix has no un-triaged empty-live cell, and the position
enumeration is catalog-derived so a newly-added expression form cannot silently escape it.*

This is the operational definition of the north star's "an obligation exists at EVERY
faultable position" — a grid you can count, and a catalog that keeps it closed as the
language grows. It would have caught BUG-031/032 **by construction**.

---

## 7. Verification discipline, independence, and model tiering

- **Source-grounded, always.** Every gap / divergence / inconsistency finding carries a
  `file:line` and a live `precept_compile` probe transcript. No document-only findings —
  rejected on sight. Grep against **committed HEAD** when this session edited the spec, so a
  disputed edit cannot self-validate.
- **MCP freshness.** Rebuild `src/Precept` + `tools/Precept.Mcp` and `/mcp reconnect precept`
  before the Phase-C probe pass; an MCP-vs-green-test disagreement means a stale server, not
  a code bug — re-probe after reconnect.
- **Independence where it is the asset.** The re-extraction diff (B), the reconciliation
  backstop (C), and the empty-live-cell adversarial pass (C′) run as **fresh independent
  agents, never forks** — a fork is the main loop (same model, same priors) and only
  launders the orchestrator's own reading back as if reviewed. Fold the needed lenses into
  one independent agent rather than nesting; keep after-the-fact verification *outside* the
  authoring agent.
- **Model tiering (cost).** Register extraction, criteria drafting from a clear claim, brief
  packaging, citation sweeps → cheap models (Haiku/Sonnet). The heavy gap analysis (C) and
  the matrix triage (C′) → **Opus, extensive**. Independent design/review passes → a fresh
  Fable agent. The worktree build agents (F) use no Fable.
- **Pre-design gate honored.** The register marks each claim's track: `implement-locked`
  (proceed — most gap-closing is implementation against already-locked spec), `needs-design`
  (genuinely new surface → owner Tier-2/3 conversation → `/design`, does not enter the
  implement track until closed), `scope-drop` (owner scope decision in A1). This keeps the
  conformance track free of new-surface design and routes the few genuinely-new items out.

---

## 8. The output artifact — a readiness plan a context-free agent can build

The pipeline's product is **not another plan**. It is a set of **work-unit briefs**, each of
which is simultaneously owner-legible and agent-executable (the artifact **is** the prompt —
plain language and defined terms serve both readers at once; jargon and label-soup fail both
at once). Each brief carries, in plain prose:

- **What it does** — a one-line plain-language description (the unit's name; **no** bare
  `Slice-8` / `W-A` / `F-LANG-COLL-06` codes as its identity — those are banned as load-
  bearing structure).
- **The spec claim(s) it satisfies** — quoted verbatim with the `§` anchor (the register row).
- **Definition of done** — its failing tests / acceptance criteria (the agent checks itself
  against these; full-compile assertions via `Compiler.Compile(...)`, never the type-checker-
  only `Check`).
- **Source-reuse pointers** — `file:line` for the catalog entry / helper / seam to extend
  (reuse, never fork a parallel definition — a new keyword goes in the catalog first).
- **Scope boundary** — what is explicitly *not* this unit's job (the adjacent claim it must
  not wander into).
- **Dependencies** — which units must land first, stated by plain description.

**Sizing and ordering.** Each unit is completable by one coding agent in one bounded run
(oversized units are split). Units are dependency-ordered where dependent and marked
independent where not, so multiple worktree-isolated agents run concurrently without
conflict (the matrix's position-catalog work is a shared substrate → it sequences first,
like v3's Slice 0, precisely because everything downstream builds on it).

**The context-free-legibility gate (E).** Before a brief ships, an independent check asks:
*"Could an agent with none of this conversation's context build exactly this from the brief
alone?"* Undefined coined terms, reliance on accumulated context, or an unquoted "you know
what I mean" claim fail the gate. The same properties make the owner able to read and sign
off in plain language — legibility and executability are the *same* property here, not a
tradeoff.

---

## 9. Definition of Done — the checkable claim

"The readiness plan is complete" is true when **all** hold, each a count or a gate, not a
judgment:

1. **Spec frozen and clean** — Phase-A commit exists; modal-verb lint has zero unaccounted
   sentences; inconsistency list is empty; boundary handoff is total and self-consistent.
2. **Register complete** — re-extraction diff is zero; register claim-set = lint hit-set.
3. **Every claim dispositioned** — zero un-dispositioned rows; each with `file:line` + a live
   probe; every prior-audit non-conformant row mapped or retired.
4. **No orphans** — every gap-claim → criterion → test → work-unit; every criterion/test/unit
   → a claim. Both directions, zero orphans.
5. **Prevention completeness** — the position × fault matrix has no un-triaged empty-live
   cell; the position enumeration is catalog-derived; every covered cell has a creation
   probe; every empty-live cell became a work-unit.
6. **Prevention soundness** — every discharge path routes unproven → reject (v3's fail-open
   sweep, retained as a subset); no verdict false-`Proves`.
7. **Runtime contract certified without runtime code** — every fault/constraint class in the
   handoff is dispositioned to exactly one side; the runtime-contract register rows are
   internally consistent. (Runtime code stays spec-only by design.)
8. **Docs self-consistent** — no surviving "Implemented" overclaim against real code; no
   fabricated citation; per-unit doc-sync landed in-commit.

**The plan is *executable* (a distinct, earlier bar)** when every work-unit brief passes the
context-free-legibility gate — that is when the owner hands the set to fresh coding agents.
DoD items 1–8 are what those agents, running the set, drive to green.

---

## 10. Where this deliberately diverges from the current v3 plan (and why)

The v3 plan's **per-slice execution rigor** — design → doc-correction-gated-before-criteria →
acceptance-criteria-before-code → fresh-worktree build → adversarial diff review → slice-
boundary pause — is sound and is **reused wholesale** as Phase F. The divergences are about
*scope* and *what gets proven*, not that machinery.

1. **Whole-compiler denominator, not a proof-MVP subset.** v3 narrowed readiness to seven
   proof-engine slices and left the completeness backbone un-owned; its DoD is "six proof
   capabilities green." This meta-plan's denominator is *every compiler-side normative spec
   claim*; the proof MVP becomes a **subset** of the register's prevention claims. **Why:**
   the north star is a complete, spec-conformant compiler — "six capabilities" is not a
   completeness statement against the spec, and the ~236 non-conformant prior-audit rows sit
   almost entirely outside the seven slices.

2. **Obligation-*creation* completeness is a first-class denominator (Phase C′), co-equal
   with discharge soundness.** v3's fail-open sweep is discharge-only and structurally cannot
   see a position that was never collected (BUG-031/032). **Why:** "an obligation exists at
   every faultable position" is half of prevention-completeness and is currently unenforced —
   no `ChildExpressionPositions` catalog exists. Catch by construction, not by tripping over.

3. **A hard spec-freeze precondition (Phase A) before anything is derived.** v3 reconciles
   docs per-slice against a spec that still embeds transient scaffolding and unresolved self-
   contradictions. **Why:** the denominator cannot move under the audit; a contradictory spec
   yields unsatisfiable acceptance criteria.

4. **The readiness plan is organized by spec-claim work-units in plain language, not by
   proof-engine-internal slice names** (`§1a single-fact`, `§3 money`). **Why:** owner-
   legibility is co-equal with agent-executability; proof-internal jargon as unit identity
   fails both the owner review and the context-free agent.

5. **A bidirectional traceability ledger spanning the whole denominator**, not only per-slice
   acceptance matrices. **Why:** "nothing was missed" must be checkable across the entire set
   (forward *and* backward orphans), which a per-slice matrix cannot assert.

**A fair reading of v3:** if the owner's intent is genuinely "ship the proof MVP first, defer
the rest," v3 is a coherent plan *for that subset* and this meta-plan's Phases A–E can be
scoped to the proof-relevant register rows as a first increment. But the brief's north star
is the whole compiler, and this methodology sizes to that.

---

## 11. Owner decision points (gates that need a human, surfaced not resolved)

- **A1 scope calls** — which specced-but-unbuilt features are *will-not-build* (remove the
  claim) vs *intended target* (keep). Owner decision; not new-surface design.
- **A2 boundary rulings** — any fault/constraint class whose side (compile-time vs ingress vs
  outside-envelope) is genuinely unsettled surfaces to the owner, not resolved in-pass.
- **A3 divergence resolutions** — where impl contradicts spec and git history does not settle
  it as drift, the owner rules which is right.
- **The freeze sign-off (A → B)** — owner signs the frozen-spec diff; the whole chain derives
  from that commit.
- **`needs-design` items** — routed to Tier-2/3 pre-design conversation + `/design` before
  entering the implement track.

This document is methodology. It produces no gap list and no code, and it settles none of the
above — it defines how they get answered with proof rather than judgment.
