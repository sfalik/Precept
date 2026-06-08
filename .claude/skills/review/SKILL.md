---
name: review
description: Stage 6 of the engineering lifecycle — the engineering-lifecycle umbrella reviewer. Reviews any stage artifact (research / design / plan / code / promotion / whole work item) by dispatching to the discipline that already owns that surface and folding the results into one verdict. The code branch does the built-in `/review`'s PR-review job (via `/code-review` + `precept-reviewer`) and intentionally subsumes — does not invoke — the built-in. Triggers on — "review this design", "review this research", "review this plan", "review this PR", "review this diff", "review this promotion", review this work, "did we complete this properly", "is this ready to close", lifecycle review, completion check, "before we sign off on X". The bare `/review <work-item>` invocation still runs the unchanged end-of-lifecycle completion check (the whole-item branch). Distinct from `/audit` (periodic drift detection) and from `precept-reviewer` (the read-only critic this skill spawns).
---

# Precept Lifecycle Review (umbrella)

A single `/review` entry reviews **any** lifecycle artifact — a research doc, a design doc, a plan, a code change/PR, a promotion, or a whole work item — by dispatching to the discipline/tool that already owns that surface and folding the results into one verdict. The bare invocation `/review <work-item>` still runs today's end-of-lifecycle completion check unchanged (the whole-item branch), and `/review <pr-or-diff>` does the PR-review job the shadowed built-in `/review` would have done — via `/code-review` + `precept-reviewer`.

The skill is a **router + fold**, not a reviewer: each branch spawns/invokes the existing owner (`precept-reviewer`, the built-in `/code-review`); the skill itself adds only the dispatch rule, the result-fold, the lifted plan-quality check, the promote doc-update-faithfulness cross-check, and the preserved completion ledger.

## When to use

- A single stage artifact needs reviewing on its own — paste its path (or name the branch) and get the owning discipline applied.
- A work item is "done" by the team's informal sense; before declaring it formally complete (the whole-item branch).
- Closing a slice, phase, or named workstream.
- Before stamping a milestone or release marker.
- After running `/promote` — to verify the promotion landed faithfully (the promote branch), or the full lifecycle ran cleanly (the whole-item branch).
- A PR or diff needs the Precept-aware review (the code branch — does the built-in `/review`'s job, better, for this repo).
- User says "review this design / research / plan / PR / promotion" or "is this ready to sign off?" / "did we complete this properly?"

## When NOT to use

- Ongoing canonical-doc drift check across all docs (use `/audit`, Phase 9).
- Pre-work design authoring (use `/design`, which has its own review at lock time).
- Generic non-Precept PR review reaching the bare built-in `/review` — the code branch intentionally subsumes it (see `## Collision: intentional subsumption`).

## Dispatch

`/review` selects exactly one branch by a **precedence-ordered** parse rule. The order is: strip a leading mode token → explicit subcommand verb → path-shape inference (clauses 1–6, first match wins) → whole-item default.

```
/review [high|ultra] [research|design|plan|code|item|promote] <target> [flags]
/review [high|ultra] <target> [flags]            # subcommand inferred from <target>
/review [high|ultra] <work-item> [--design <doc>] [--plan <doc>] [--strict] [--accept-debt "reason"]
```

**The precise parse rule.** After stripping a leading `high|ultra` token, inspect `token[0]`:

1. **If `token[0] ∈ {research, design, plan, code, item, promote}` AND `token[0]` is not an existing path** — treat it as the explicit subcommand; the remainder is target + flags. The subcommand is the escape hatch and always wins over inference.

2. **Else classify the positional target by path-shape**, evaluating these clauses **in this fixed precedence order, first match wins**. Filename/location signals outrank content-section signals (a plan doc legitimately *contains* a `## Decisions` section per the plan skill, so a `## Decisions`-presence test is **not** a safe discriminator):

   1. **`research/` path → research branch.** Location is unambiguous.
   2. **Plan-filename (`*-plan-*.md` / `*readiness-plan*.md`), OR a `docs/Working/` doc whose dominant structure is `## Phase` rows → plan branch.** Tested *before* any `## Decisions` check: every in-repo plan doc carries both a `*-plan-*` filename and a `## Decisions` section, so keying design-vs-plan on `## Decisions`-presence would route every real plan doc to the design branch and make the plan branch's lifted exit-criteria check unreachable. Filename/location is the strong signal.
   3. **`#N` / `PR N` / `diff` / `HEAD` / a dirty working tree / a path under `src/`/`tools/`/`test/` → code branch.**
   4. **A `docs/Working/*.md` not matched by clauses 1–3 → design branch** — a Working-doc that is not a plan-filename and not a research path is a design *by elimination of location + kind*, regardless of whether a `## Decisions` section is present yet. (An in-progress design that has not yet written its `## Decisions` section still routes here — where "missing `## Decisions`" becomes a *finding the branch reports*, never a router input that drops the doc through to whole-item. A design doc carrying a freshly-added `**Promoted to:** <link>` header routes to **promote** instead — see clause 5.)
   5. **A design doc carrying a fresh `**Promoted to:** <link>` header, OR a committed `docs/*.md` outside `Working/` whose change is a promotion landing → promote branch** (see `### promote` and `## Branches`).
   6. **Else (no recognized target, or a slug resolving to no single file) → whole-item.**

3. **Else → whole-item** (the no-stage-target default).

**Tiebreak when two path-shape clauses still co-match.** The clause order above *is* the tiebreak: lower-numbered clause wins, and within that, filename/location signals outrank content-section signals. So `docs/Working/foo-plan-2026-XX.md` with a `## Decisions` section resolves to **plan** (clause 2) over design (clause 4). This makes "the inferred branch" well-defined, never undefined.

**Ambiguity fallback (no modal).** When two clauses genuinely co-match (e.g., a design-doc that embeds a `## Phase` block) or the target resolves to nothing, state in one plain-text line **which** clause was inferred (per the precedence above) **and** the runner-up, then proceed with the inferred branch unless the author's phrasing contradicts it — **never block on a modal**. A nonexistent path is announced and treated as a work-item slug → whole-item. Because the precedence order resolves the *selection* deterministically, the fallback line only *announces* it; it never invents a winner.

Example fallback line: `inferred design (clause 4); runner-up plan — re-run with /review plan <path> to plan-check.`

**Known-weak case (documented so the author reaches for the subcommand).** Bare-path inference on a plan doc relies on the `*-plan-*` filename convention. A plan doc that does *not* follow it (or a design doc you want reviewed *as* a plan) should be reviewed via the explicit `/review plan <path>` subcommand rather than trusting inference. This is the interim measure until `/plan` emits a frontmatter `lifecycle-stage:` marker, at which point design/plan disambiguation keys on the marker, not on filename or `## Decisions`-presence.

## Branches

Six branches. Each is a thin orchestration spec — it spawns/invokes the existing owner and the skill adds only the small glue named below. Spawn directives reference `precept-reviewer`'s paths **by capability name** (a sibling agent's internal section numbers rot under renumbering); a section number, if mentioned at all, is a non-load-bearing hint.

### research

Spawn `precept-reviewer` scoped `Review research file <path>`, which exercises its **Stage-1 Research-Doc Review Path** — the path that mechanizes the research skill's 10 behavioral guards via grep (status/external-engagement/authored frontmatter; Methodology / Findings / Threats to Validity / What-would-change-this / Sources sections; per-citation excerpt + stable-identifier + access-date + source-grade discipline; source-verification of ≥3 citations; promote-or-cite; sub-folder taxonomy). Fold its findings into the verdict.

**Contingency (resolves the buildability gate).** The branch dispatches to that path *if it exists*. If `precept-reviewer`'s Research-Doc Review Path is ever retired, the branch's fallback is to **inline the research skill's 10 behavioral guards as a grep checklist run by the skill itself** (the same grep mechanization the path performs) rather than dispatching. Either way the research-artifact discipline is applied; the branch is buildable regardless.

### design

Spawn `precept-reviewer` scoped `Review design doc <path>`, which exercises its **design-doc review path** (the Independent re-statement preamble + framing comparison; four-leg / stakes-based-rigor leg-checking; citation discipline; Philosophy Alignment principle-coverage matrix; Language Design Grounding; Semantic Rules; Audience and Teachability; Architecture Grounding; Source Verification; comparator-checking). Fold its findings into the verdict.

A **missing `## Decisions` section** is reported by this branch as a *finding* (the design is incomplete) — it is never used as a router input. An in-progress design with no `## Decisions` section yet still routes here (per dispatch clause 4) and the missing-decisions condition surfaces as a finding.

### plan

Run the **skill-owned lifted four-element exit-criteria check** (relocated from the whole-item ledger): for each phase the plan touches, exit criteria — in checklist *or* narrative form — must contain all four elements: **(i)** completion marker (✅/❌ or `Complete YYYY-MM-DD`), **(ii)** test-suite outcome (counts or "test suite green"), **(iii)** commit references (per workstream or rolled-up), **(iv)** workstream enumeration with per-workstream one-line outcome. A phase missing any of the four drops to ⚠️ with the missing element named. A phase with no exit criteria in any form is 🔴. Decisions surfaced as gates (named + resolution-location cited) — buried decisions ("figure out X during execution") are 🔴.

Then spawn `precept-reviewer` scoped to the plan/PR files for process discipline. Fold both into the verdict.

### code

Invoke `/code-review` (effort from the parsed mode) **and** spawn `precept-reviewer` scoped to the diff. This branch **does the PR-review job the built-in `/review` would have done** — it does **not** invoke the built-in (see `## Collision: intentional subsumption`). Emit the point-of-use banner: `umbrella /review: dispatching to /code-review + precept-reviewer; the built-in PR-review is intentionally subsumed.`

Fold `/code-review`'s correctness/cleanup findings and `precept-reviewer`'s catalog/doc-sync/language-propagation/MCP/test/rationale findings into one verdict, deduping same-`file:line` findings per `## Result fold` (with the degrade-gracefully fallback when `/code-review`'s findings are not `file:line`-keyed).

Until `/audit` ships, this branch's doc-sync lens is the interim path for committed-canonical-doc drift, with a concern-flag toward audit.

### promote

Review a single **promotion artifact**: a design doc carrying a freshly-added `**Promoted to:** <link>` header plus the canonical-doc edits it cites. This is the standalone single-artifact mirror of the cross-stage Stage-5 ledger the whole-item branch runs — for the in-flight promotion the whole-item branch can't serve (it needs a finished work item).

Spawn `precept-reviewer` scoped to the canonical-doc diff under its **doc-sync lens** (why-content faithfulness: the rationale the design locked actually transferred to canonical, not merely a pointer; stale-doc findings), folded with a **promote-specific check the skill owns**:

- Every entry in the source design's `## Doc-update enumeration` was actually touched by the promotion → 🔴 for any enumerated canonical doc not touched.
- The design's `**Promoted to:** <link>` resolves to the canonical home → 🔴 if it does not.
- The archive cross-link from the canonical doc back to the archived design resolves → 🔴 if it does not.

A clean promotion (every enumerated doc touched, links resolve, why-content present in canonical) returns ✅.

### whole-item

The **preserved** completion-check — reached as the no-stage-target default (the back-compat hinge). Its body is unchanged. By default it runs today's lighter presence + gate checks; it **deep-dispatches the per-stage branches (research / design / plan / code / promote) on present artifacts only under `--strict`, `high`, or `ultra`** — neutralizing the fan-out cost of a routine sign-off. `--strict` / `--accept-debt` / debt-log are scoped to **this branch only** (a single artifact has no "lifecycle debt" to defer).

```
/review <work-item> [--design <design-doc>] [--plan <plan-doc>] [--strict] [--accept-debt "reason"]
```

The skill verifies the work item was processed through all 5 earlier stages:

1. **Stage 1 — Research**
   - Was research done? Check for `research/<area>/` docs cited in the design or plan row.
   - **Three honest exits** — exactly one applies per work item:
     - **(a) Research-cited**: design or plan row cites at least one file in `research/` with an excerpt or section reference. ✅
     - **(b) Research-not-applicable**: design or plan row declares `research-status: not-applicable — <one-line reason>` (e.g., "direct bug fix against `proof-engine.md § N`; no comparator question," "mechanical sample-corpus restore; no design surface"). ✅ The declaration is the honest "I checked, this doesn't apply here" answer — accepted at face value when the work is genuinely mechanical (bug fixes, test-fixture restores, doc-only sweeps, refactors with no behavior change).
     - **(c) Research-missing**: neither (a) nor (b) is present, but the work item involves design surface (new language construct, new diagnostic, new pipeline stage, new public API). ⚠️ "no research cited — intentional?" — owner judges.
   - The skill refuses to upgrade (c) to ✅ without either a research citation or an explicit not-applicable declaration. Marking a work item not-applicable retroactively is fine; silently ignoring missing research is not.

2. **Stage 2 — Design**
   - Design doc exists in `docs/Working/` or `docs/Working/Archive/`
   - Status declared "Locked YYYY-MM-DD"
   - Every decision has four-leg structure (Rationale + Alternatives + Precedent + Tradeoff)
   - Acceptance criteria are testable (not prose)
   - Doc-update enumeration is present
   - **Spec-first verification**: for each decision / claimed gap / claimed "new surface", grep the canonical spec/design and confirm the claim holds. 🔴 if the design frames as an "open decision" something the canonical spec already settles (cite the settling section), or claims "new surface" for something the spec already documents (pre-existing — it's an implementation gap, not new surface), or claims a "gap" the spec already fills. ⚠️ if a decision's `Sources consulted` leg shows no check of the canonical spec for prior settlement. (This is the lifecycle-6 mirror of the precept-reviewer "pre-existing-vs-new" discipline and the design skill's guard 17.)
   - 🔴 if any of these are missing

3. **Stage 3 — Plan**
   - Plan doc exists (compiler-readiness-plan, feature-plan, etc.)
   - Phases defined with exit criteria
   - Decisions surfaced as gates (not buried in execution steps)
   - Doc-touch obligations per phase enumerated
   - 🔴 if any phase the work item touches lacks exit criteria
   - **Exit-criteria format is not prescribed.** Discrete checklists (`- [x] foo`) and narrative phase-row prose both satisfy the gate, provided the prose contains all four elements: **(i) completion marker** (✅/❌ or `Complete YYYY-MM-DD`), **(ii) test-suite outcome** (counts or "test suite green"), **(iii) commit references** (per workstream or rolled-up), **(iv) workstream enumeration** with per-workstream one-line outcome. A narrative row missing any of these four elements drops to ⚠️ (the missing element is named in the report).
   - Decisions surfaced as gates means: where the phase requires a decision before execution can proceed (e.g., locked design doc, choice between option A/B), the decision is named and its resolution location cited. Buried decisions ("we'll figure out X during execution") drop to 🔴 — execution-time invention isn't a planned decision.

4. **Stage 4 — Execute**
   - Code shipped (commit refs in design or plan)
   - Tests added or extended (count present in design's acceptance criteria? satisfied?)
   - Build clean (`dotnet build` 0 warnings if scoped to .NET work)
   - 🔴 if tests are missing or build is dirty

5. **Stage 5 — Promote**
   - Canonical doc(s) updated with why-content from the design (per design's doc-update enumeration)
   - Archive header on design doc: `**Promoted to:** <link>` (or explicit historical status)
   - 🔴 if any enumerated canonical doc not touched OR archive header missing

6. **Acceptance criteria**
   - From design doc, demonstrably satisfied (test refs, screenshots, MCP probes, etc.)
   - 🔴 if any acceptance criterion has no evidence of satisfaction

7. **Catalog-discipline + doc-sync spot-check**
   - Spawn `precept-reviewer` agent against all files touched by the work item
   - Fold its findings into the report
   - 🔴 if precept-reviewer surfaces P0/P1 findings

**Output**: completion report at `docs/Working/lifecycle-review-<work-item>-YYYY-MM-DD.md` with per-stage ✅/⚠️/🔴 status, summary verdict (Ready to sign off / Remediation required / Owner judgment needed), and remediation items for each ⚠️/🔴.

## Result fold

The one piece of genuinely-new glue the skill owns: each branch's child tools speak different native severity vocabularies; the fold normalizes them into **one 🔴/⚠️/✅ scale + one verdict line**. No existing tool owns this merge.

**Severity-normalization map:**

| Source severity | Folds to |
|---|---|
| `precept-reviewer` BLOCKER | 🔴 |
| `precept-reviewer` CONCERN | ⚠️ |
| `precept-reviewer` NIT | note |
| `/code-review` issue (high-confidence) — correctness bug | 🔴 |
| `/code-review` issue (high-confidence) — cleanup/simplification | ⚠️ |
| completion-check 🔴 / ⚠️ / ✅ | pass through unchanged |

**One verdict line** reuses the three verdicts: **Ready to sign off** / **Remediation required** / **Owner judgment needed**.

**`--strict` generalizes.** It promotes every ⚠️ → 🔴 across the whole folded output (not only the whole-item branch), extending the current single-branch `--strict` cleanly. (Per the flag matrix below, `--strict` is accepted only on the whole-item branch; the generalization is how its promotion applies to any deep-dispatched per-stage findings under `--strict`/high/ultra.)

**Code-branch dedup with degrade-gracefully fallback.** On the code branch, `/code-review` (correctness) and `precept-reviewer` (catalog/doc-sync) can flag the same location from two lenses. The fold collapses same-`file:line` + finding-equivalence into one entry showing both lenses, so one underlying issue is not double-reported. Equivalence is keyed conservatively on identical `file:line` + matching rule reference, not fuzzy text — the merged entry preserves both lenses' text so nothing is lost.

This dedup is **contingent on `/code-review` emitting `file:line`-keyed findings**. `precept-reviewer`'s `file:line` format is verified; `/code-review` is a built-in command whose exact finding schema is not yet inspected against a real sample. **Fallback rule:** if `/code-review`'s findings cannot be `file:line`-keyed, the fold **does not merge** — it prints the two lenses in separate labelled sections rather than attempting a dedup the interface can't support. If `/code-review` does not tag bug-vs-cleanup, the 🔴/⚠️ split for its findings **defaults to ⚠️** (surfaced for human triage) rather than guessing 🔴.

**Per-branch valid-flag matrix.** An inapplicable flag is **rejected with a one-line message**, never silently swallowed:

| Flag | Valid only on |
|---|---|
| `--fix` / `--comment` | code |
| `--strict` / `--accept-debt` | whole-item |
| `--design` / `--plan` | whole-item |
| (no special flags) | research / design / plan / promote |

The `promote` branch takes no special flags (a single artifact has no lifecycle debt and applies no fixes).

## Read-only contract

The skill is **read-only on every branch** — it produces a report; it does not edit the tree or post comments. `precept-reviewer` is read-only (a critic, not a fixer), and the completion check is read-only.

`--fix` / `--comment` are **opt-in pass-throughs to `/code-review` only**, valid solely on the code branch (per the flag matrix), and the author must request them explicitly (e.g. `/review code <pr> -- --fix`). The code branch invokes `/code-review` **without** `--fix`/`--comment` by default. Default mutation is never the behavior of a review umbrella.

## Collision: intentional subsumption

The project skill named `review` **shadows the built-in `/review`** — and this is **deliberate, not a bug**. The code branch dispatches PR/diff targets to `/code-review` + `precept-reviewer`, which is a strict superset of the generic built-in `/review` for this repo (bug-hunting **plus** Precept's catalog/doc-sync/language-propagation/MCP/test/rationale discipline). The project skill now legitimately does the PR-review job — and does it better here — so the shadow is correct-by-construction.

The built-in `/review` is **subsumed, not invoked**: no branch calls it. What the code branch reaches is a Precept-aware *substitute* that strictly dominates the generic built-in for this repo.

**Do not "fix" this shadow by renaming or disabling the skill.** The shadow is the design. The code branch emits a point-of-use banner so the subsumption is discoverable at the moment it happens. A future maintainer who finds the shadow surprising should read this section before reaching for a rename.

## Migration

Back-compat: today's invocation lands on the **identical** whole-item path. `/review <work-item> [--design <doc>] [--plan <doc>] [--strict] [--accept-debt "reason"]` continues to work unchanged — `<work-item>` is a slug, not a leading branch verb and not a single-artifact path, so the parse rule (`## Dispatch`) routes it to whole-item; `--design`/`--plan` remain anchors/context for the whole-item branch (and now additionally seed the design/plan sub-branch dispatch under `--strict`/high/ultra); `--strict`/`--accept-debt` remain whole-item flags; the output path `docs/Working/lifecycle-review-<work-item>-YYYY-MM-DD.md` and the debt log `docs/Working/lifecycle-debt-log.md` are unchanged.

Genuine behavior *changes* for callers, all strict improvements:
- `/review <single-artifact-path>` now does single-stage review instead of treating the path as an ill-fitting work-item string.
- `/review #N` now routes to the code branch (the intended subsumption) instead of misfiring as a work-item.
- The built-in `/review` is now reached only via the code branch's dispatch (subsumed, not invoked).

The one genuinely new ambiguity — a work item literally named `research`/`design`/`plan`/`code`/`item`/`promote` — is handled by the explicit `/review item <name>` escape.

## High & Ultra Modes

**Trigger:** `/review high <args>` or `/review ultra <args>` (also recognise "high-rigour"/"ultra" phrasing in the request). **Opt-in only** — these spend many sub-agents and tokens; they are never the default. Reach for them on high-stakes, hard-to-reverse, or easy-to-get-subtly-wrong work where a single pass is not enough.

Both modes run this skill as a multi-agent `Workflow` instead of a single inline pass, and add independent multiplicity + adversarial verification *on top of* this skill's normal discipline — which still fully applies (nothing below replaces the required structure, gates, or checks). Every spawned agent works fluency-first and verifies its claims against source.

- **high** — a multi-lens reviewer panel (perspective-diverse: catalog discipline / doc-sync / language-surface propagation / soundness / etc.) → each finding adversarially verified (a skeptic tries to refute it) → one completeness critic ("what dimension was not reviewed?").
- **ultra** — more lenses, **replicate the highest-severity findings' verification**, and loop the completeness critic + adversarial falsification until dry.

**Mode propagation.** The mode token is parsed first (`## Dispatch`) and **passed to whichever child the branch dispatches to**: `precept-reviewer` receives high/ultra; `/code-review` receives the corresponding effort level. On the whole-item branch, `high`/`ultra` (like `--strict`) trigger deep-dispatch of the per-stage branches on present artifacts.

---

## Behavioral guards

### Whole-item branch guards

1. **No "Lifecycle complete" verdict if any 🔴.** Author either remediates or invokes `--accept-debt` flag with explicit reasoning ("acceptance criterion 3 deferred to Phase N because…").

2. **⚠️ statuses surface owner-judgment questions explicitly.** Not auto-resolved. The skill produces clear prompts for owner ("Was Stage 1 research intentionally skipped here? Y/N + rationale").

3. **`--strict` mode treats ⚠️ as 🔴.** For high-stakes milestones (e.g., production release marker), forces explicit answer to every judgment question.

4. **`--accept-debt` flag is captured and recorded.** Any "remediation deferred" goes into a debt log (`docs/Working/lifecycle-debt-log.md` or per-project equivalent) so deferrals don't vanish into the void.

5. **Skill is read-only.** Produces report; doesn't apply fixes. Author uses the report to drive remediation work, then re-runs the skill.

### Dispatcher guards (all branches)

6. **No silent mis-route.** Every inference announces the inferred branch (and the runner-up when two clauses co-match) in one plain-text line before proceeding. A dispatch the author can't see is a dispatch the author can't correct.

7. **Filename/location-first precedence.** Path-shape inference evaluates the clauses in fixed precedence order (research-location → plan-filename → code → Working-doc-design → promote → whole-item); filename/location signals outrank content-section signals. A `## Decisions`-presence test is never used to discriminate plan-vs-design.

8. **Reject inapplicable flags loudly.** A flag outside the per-branch valid-flag matrix is rejected with a one-line message naming the branch it belongs to (e.g. "`--fix` applies to the code branch only"), never silently ignored.

9. **Read-only by default on every branch.** `--fix`/`--comment` are explicit opt-in pass-throughs to `/code-review`, valid only on the code branch.

## Anti-patterns to refuse

- Mark "complete" with 🔴 status (must remediate or explicitly accept debt) — whole-item.
- Skip the precept-reviewer spawn on the branch that calls for it (catalog/research/design/doc-sync discipline must be verified).
- Skip acceptance-criteria verification on whole-item (the design said this passes; the review must verify it passes).
- Accept "✅ all green" without evidence (the report should cite test files, commit refs, MCP probe outputs).
- Route a plan doc to the design branch on a `## Decisions`-presence test (the precedence guard exists precisely to prevent this).
- Apply fixes or post comments by default on any branch (read-only contract).
- "Fix" the intentional built-in `/review` shadow by renaming the skill (the collision is deliberate — see `## Collision: intentional subsumption`).

## Composability

- **Input** (whole-item): `--design <design-doc>` (recommended — anchors the review to a specific design); `--plan <plan-doc>` (optional — adds phase-scope context); `--strict` flag; `--accept-debt` flag with rationale. **Input** (per-stage branches): a single artifact path or the explicit subcommand; `--fix`/`--comment` on the code branch only.
- **Output** (whole-item): completion report at `docs/Working/lifecycle-review-<work-item>-YYYY-MM-DD.md`. **Output** (per-stage branches): one folded verdict (🔴/⚠️/✅ + verdict line) inline.
- **Pairs with**: `/promote` (the promote branch reviews a promotion in isolation; the whole-item branch verifies the full lifecycle after promotion); `precept-reviewer` (spawned internally by every branch that reviews content); `/code-review` (invoked by the code branch).

Distinct from:
- `precept-reviewer` — the read-only critic this skill *spawns*; it runs against code/diff/docs in real-time and applies the per-artifact disciplines, but does not own the dispatch, the fold, the lifted plan check, or the cross-stage completion ledger.
- `/audit` — runs against all canonical docs periodically, checks for drift over time. The code/promote branches' doc-sync lens is the interim drift path until audit ships.
- `/code-review`, `/security-review` — focused on code (bug-hunting / security), not the umbrella's dispatch + fold; the code branch *invokes* `/code-review`; `/security-review` stays a directly-invokable sibling.

## Quick reference

| Symptom | Skill response |
|---|---|
| `/review research/<x>.md` with no `## Methodology` / `## Threats to Validity` | Research branch → BLOCKER in the folded report (via the Stage-1 Research-Doc Review Path) |
| `/review docs/Working/<design>.md` missing four-leg on a decision | Design branch → BLOCKER in the folded report |
| In-progress design with no `## Decisions` section yet | Routes to design branch (clause 4), reports missing-decisions as a *finding* — never drops through to whole-item |
| `/review <x>-plan-2026-XX.md` (real plan doc with both `*-plan-*` filename and a `## Decisions` section) | Routes to **plan** branch (clause 2 before clause 4); runs the exit-criteria check — a route to design here is a failure |
| Phase row narrative missing one of the four exit-criteria elements | ⚠️ — name the missing element ("no test-suite outcome stated" / "no commit refs cited") |
| Phase has no exit criteria in any form | 🔴 — Stage 3 incomplete |
| `/review #N` / `/review HEAD` / path under `src/`,`tools/`,`test/` | Code branch → `/code-review` + `precept-reviewer`; does the built-in's PR-review job (built-in not invoked); deduped on shared `file:line` (or sectioned if not line-keyed) |
| `/review <design-with-fresh-**Promoted to:**-header>` | Promote branch → doc-sync lens + doc-update-enumeration cross-check |
| Enumerated canonical doc in the design's `## Doc-update enumeration` not touched by the promotion | 🔴 — promote branch |
| Clean promotion (every enumerated doc touched, links resolve, why-content present) | ✅ — promote branch |
| Ambiguous target (design-doc embedding a `## Phase` block) | One-line "inferred design (clause 4); runner-up plan — re-run with `/review plan <path>`" note, then proceed (no modal) |
| `/review high design <path>` / `/review ultra #N` | Mode parsed first, passed to the dispatched child (precept-reviewer high/ultra; `/code-review` effort) |
| `/review design <path> -- --fix` | Rejected: "`--fix` applies to the code branch only" (not silently ignored) |
| Design doc has no acceptance criteria | 🔴 — Stage 2 incomplete; remediate before signing off (whole-item) |
| Plan doesn't enumerate doc-touch | 🔴 — Stage 3 incomplete (whole-item) |
| Design frames an "open decision" / "new surface" / "gap" the canonical spec already settles or documents | 🔴 — grep + cite the settling spec section; it's implementation against locked spec (or a Tier-3 override), not a fresh decision/surface |
| A decision's `Sources consulted` shows no check of the canonical spec for prior settlement | ⚠️ — ask for the quote or an explicit "spec silent on this" |
| Canonical doc not updated per plan's doc-touch list | 🔴 — Stage 5 incomplete; run `/promote` for the gap (whole-item) |
| Tests exist but acceptance criterion has no test ref | ⚠️ — surface judgment question |
| No research cited for a small bug fix, AND no `research-status: not-applicable` declaration | ⚠️ — likely OK; ask owner OR add a one-line `research-status: not-applicable — <reason>` to the design / plan row to convert to ✅ |
| `research-status: not-applicable — <reason>` declared on a bug fix, refactor, doc sweep, or test-fixture restore | ✅ — honest exit accepted at face value |
| `research-status: not-applicable` declared on work that introduces design surface (new construct, diagnostic, pipeline stage) | ⚠️ — challenge: the declaration looks dishonest given the surface. Ask owner whether research truly doesn't apply or whether it was skipped |
| `--strict` mode + ⚠️ present | Treat as 🔴 (generalizes across the whole folded output) |
| User invokes `--accept-debt "reason"` | Record in debt log; downgrade 🔴 to ✅* (whole-item) |
