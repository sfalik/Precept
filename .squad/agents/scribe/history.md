## 2026-04-11T00:00:00Z - Slice 9 language design doc sync (#25, #27, #29)

- Updated `docs/PreceptLanguageDesign.md`: grammar BNF (ConstraintSuffix, ScalarType), reserved keywords, scalar types section (integer/decimal/choice), field-level constraints table (maxplaces/ordered), built-in functions subsection (round()), and diagnostic codes (C60–C66).
- `docs/McpServerDesign.md`: confirmed already complete (Newman's Slice 6 commit). No changes needed.
- `README.md`: no dedicated types/constraints table exists; no update needed.
- `research/language/README.md`: #25, #27, #29 already present in domain index and open proposal issue map. No update needed.
- **Docs gap noted:** The `## Status` section at the end of `PreceptLanguageDesign.md` lists locked design decisions but does not mention integer, decimal, or choice types being locked. These should be added to the locked list in a follow-up pass if the team wants the status section kept current.

---

## 2026-04-08T13:37:07Z - Issue #17 wording clarification recorded

- Wrote a brief session log and a canonical decision entry for the Issue #17 review consensus.
- Recorded that the disagreement is wording ambiguity around "constraints," not a semantic contradiction in the computed-fields proposal.
- Captured the wording direction: computed fields cannot have field-level constraint declarations attached, but remain readable in guards, invariants, and state asserts.
- Touched only `.squad/` logging and decision files; `.squad/decisions/inbox/` was already empty.

---

## 2026-05-18T00:25:00Z - README image-width adjustment recorded

- Wrote orchestration logs for Kramer and Elaine plus a brief session log for the README DSL hero width pass.
- Merged 5 related README sizing/width inbox notes into `.squad/decisions.md`, updated affected history references, and cleared the processed inbox files.
- Preserved the final contract: GitHub repo README image cap about 830px, regenerated source width 1660px at 2×, and native text remains the only fully robust typographic match.

---

## 2026-04-05T03:04:51Z - Early history expansion recorded

- Wrote orchestration and session logs for J. Peterman's `docs/HowWeGotHere.md` early-period expansion and the related `repo-journey-summary` skill update.
- Checked `.squad\decisions\inbox\` (0 files), so `decisions.md` stayed unchanged and no archive move was needed.
- Updated the affected agent histories; no `history.md` files exceeded the summarization threshold.

---

2026-04-05T03:20:00Z: Steinbrenner applied branch protection to main (pull requests required, force pushes/admin only, no branch deletion).

## 2026-04-05T00:06:16Z - README hero PNG fallback recorded

- Wrote orchestration and session logs for J. Peterman's README hero PNG fallback and fallback DSL block.
- Merged 1 decision inbox item into decisions.md, deduplicated as needed.
- Updated affected agent histories and cleared the processed inbox file.

---

## 2026-04-05T02:54:36Z - Trunk analysis recorded

- Wrote orchestration and session logs for Frank, Steinbrenner, Uncle Leo, and J. Peterman.
- Merged 4 decision inbox item(s) into decisions.md, skipped 0 duplicate(s), and archived 0 decision entries older than 30 days.
- Updated the affected agent histories, cleared the processed inbox files, and recorded the return-to-main strategy summary.

---

## 2026-04-05T02:59:27Z - API evolution clarification recorded

- Wrote orchestration and session logs for J. Peterman's `docs/HowWeGotHere.md` chronology clarification.
- Merged 1 decision inbox item into decisions.md, skipped 0 duplicate(s), and archived 0 decision entries older than 30 days.
- Updated the affected agent histories and cleared the processed inbox file.

---

## 2026-04-05T02:32:19Z - README inline DSL cleanup recorded

- Wrote orchestration and session logs for J. Peterman's README quick-example cleanup.
- Merged 2 decision inbox item(s) into decisions.md, skipped 0 duplicate(s), and archived 0 decision entries older than 30 days.
- Updated affected agent history for J. Peterman and cleared the processed inbox files.

---

## 2026-04-04T23:02:57Z - Hero snippet pass recorded

- Logged J. Peterman's canonical hero-snippet pass in orchestration and session logs.
- Merged decision inbox into decisions.md (27 appended, 1 deduplicated/skipped) and cleared processed inbox files.
- Propagated the hero-snippet source-of-truth update to Elaine and Steinbrenner.
- Summarized oversized agent histories into compact Core Context sections.

---

## 2026-04-04

### Gold Brand Mark Exception
- Orchestration, session, and decision logs created for Elaine's update clarifying the Gold accent exception in the combined brand mark.
- Decision merged into decisions.md and inbox cleared.

T20:37:52Z - Scribe: Model Policy Decision Merged

Processed user directive to always use latest available model versions. Merged 2 decisions from inbox to decisions.md:
- Model Policy: Latest version selection (automatic routing)
- Elaine's Mapping Table Visual Unification decision

Deleted inbox files. Appended updates to agent histories.

---

# Project Context

- Owner: shane
- Project: Precept - domain integrity engine for .NET. DSL that makes invalid states structurally impossible.
- Stack: C# / .NET 10.0, TypeScript, xUnit + FluentAssertions
- Components: src/Precept/, tools/Precept.LanguageServer/, tools/Precept.Mcp/, tools/Precept.VsCode/, tools/Precept.Plugin/
- Universe: Seinfeld
- Created: 2026-04-04

## Core Context

Team cast on 2026-04-04: Frank (Lead), George (Runtime), Kramer (Tooling), Elaine (MCP/AI), Soup Nazi (Tester), Uncle Leo (Code Reviewer), J. Peterman (Brand/DevRel), Steinbrenner (PM).

## Recent Updates

### 2026-05-01T06:21:31Z - Annotation-bridge design batch recorded
- Ran the hard-gate 7-day archive pass because `decisions.md` exceeded 51200B before merge.
- Merged 6 inbox files into 2 canonical decision entries, wrote orchestration/session logs for frank-6 / george-2 / george-3, updated Frank and George histories, and cleared the processed inbox files.
- Health report: `decisions.md` 53785B -> 38562B; no `history.md` crossed the 15 KB summarization threshold after propagation.


### 2026-05-01T06:04:34Z - Parser coverage assertion exploration recorded
- Ran the hard-gate 7-day decision archive pass (archived 1 active entries) because decisions.md was 518013B before merge.
- Merged 1 unique Frank inbox record into canonical decisions, cleared 1 inbox file, and wrote the Frank orchestration/session logs for the parser coverage assertion batch.
- Health report: decisions.md 518013B -> 535361B; no history.md crossed the 15 KB summarization threshold after propagation.

### 2026-04-29T05:00:52Z - Collection iteration/rules research recorded
- Wrote orchestration logs for frank-6 and frank-7 plus the session log for the paired collection research batch.
- Archived 0 decision entries older than 7 days, merged 2 inbox records into canonical `decisions.md` entries, and cleared the processed inbox files.
- `decisions.md` size: 190331B -> 229312B; summarized Frank's oversized history and refreshed durable squad state.

### 2026-04-29T04:47:14Z - Principles/vision closeout recorded
- Wrote orchestration logs for frank-3, frank-4, and frank-5 plus a one-paragraph session log for the spec-principles / vision-archive closeout batch.
- Merged 6 inbox records into 2 canonical `decisions.md` entries, deduplicating the earlier archive-readiness audit and clearing the processed inbox files.
- Updated Scribe's durable session memory; no decision-archive move was needed because there were no 30-day archive candidates in this batch.

### 2026-04-27T00:19:02Z - Combined design v2 closeout recorded
- Wrote the session log for the v2 architecture closeout after Frank's revision and George's final approval.
- Merged 17 related architecture inbox notes/directives into one canonical `decisions.md` entry and cleared the processed v2 design inbox files.
- Updated Frank and George history with the approved boundary set and the remaining stage-level follow-up targets.

### 2026-04-12T19:45:41Z - Squad `@copilot` lane retirement recorded
- Wrote orchestration logs for Newman and Frank plus a session log for the Squad operations contract change.
- Merged 2 relevant inbox items into one canonical `decisions.md` entry, recording the lane retirement and marking the earlier direct-`@copilot` directive as superseded.
- Updated affected cross-agent memory for Frank and Ralph, then cleared the processed inbox files.

### 2026-04-08T13:29:23Z - Language research corpus closeout recorded
- Merged the remaining `.squad/decisions/inbox/` backlog into `.squad/decisions.md`, recorded 61 entries, skipped 2 superseded duplicate(s), and cleared the inbox.
- Wrote the missing closeout orchestration/session logs, updated `now.md` plus the Frank/George/Steinbrenner histories, and captured the finishing corpus commit `3cc5343`.
- This pass touched only `.squad/` bookkeeping so the branch can return to a clean state.

### 2026-04-08T07:16:23Z - Language research corpus recorded
- Wrote orchestration logs for Frank, Steinbrenner, and George plus a session log for the language research corpus pass.
- Merged 3 language-research inbox notes into `.squad/decisions.md`, corrected the canonical map reference back to `docs/research/language/domain-map.md`, and deleted the processed inbox files.
- Updated the affected agent histories. No decision-archive move or history summarization was needed; Batch 3 research and the final README/index sweep are still outstanding.

### 2026-04-06T00:48:04Z - Kramer Concept 26 bounded configurability recorded
- Wrote a lightweight session log capturing Kramer's Concept 26 direction: layout presets, split resizing, and per-panel collapse without freeform docking.
- No decision inbox merge was needed, and no product mockup files or preview index files were touched in this pass.

### 2026-04-05T16-16-48Z - Rule proposal consolidation recorded
- Wrote orchestration/session records for Frank, George, J. Peterman, and Steinbrenner; merged the inbox notes into .squad/decisions.md, cleared .squad/decisions/inbox/, and updated the affected histories.
- Archived older decisions as needed; no agent history crossed the summarization threshold after this pass.
### 2026-04-05 - DSL compactness label rollout recorded
- Wrote the Steinbrenner orchestration log and the dsl-compactness session log, merged the directive plus rollout records into .squad/decisions.md, and cleared .squad/decisions/inbox/.
- Checked archive and summarization thresholds: 0 decision entries older than 30 days archived, and 0 history.md files required summarization.

### 2026-04-05 - Proposal expansion consolidation recorded
- Wrote orchestration/session logs for Steinbrenner and Frank, merged 3 inbox records into .squad/decisions.md, cleared .squad/decisions/inbox/, and updated the affected agent histories.
- Decisions archive unchanged (0 entries older than 30 days), and no history.md file crossed the summarization threshold.

### 2026-04-05 - Elaine diagram-transitions mockup recorded
- Logged Elaine's in-diagram transitions exploration and wrote the orchestration/session records.
- Merged 5 preview-panel decision inbox item(s) into `.squad/decisions.md`, cleared `.squad/decisions/inbox/`, and archived 0 older decision entries.
- Preserved the related Elaine/Kramer/Steinbrenner history updates already in flight; no `history.md` files crossed the summarization threshold.

- Team initialized on 2026-04-04 with the full eight-agent Seinfeld cast across project domains.

### 2026-04-05 - Frank language proposal review recorded
- Wrote orchestration and session logs for Frank's GitHub issue review across proposals #8-#13.
- Merged 4 decision inbox item(s) into .squad/decisions.md, cleared .squad/decisions/inbox/, updated affected histories, and archived 0 older decision entries.


### 2026-05-01T05:51:14Z — Catalog vision wording batch recorded
- Logged Frank's catalog-vision wording pass, ran the 7-day decision archive gate, merged 11 unique inbox records, and cleared 11 processed inbox files.
- Health: decisions.md 459991B -> 518013B, decisions-archive.md now 57602B, duplicates skipped 0, and no history.md file required summarization.
