---
name: "multi-researcher-synthesis"
description: "How to synthesize multiple research passes (brand, PM, UX) into a single concrete proposal with clear constraint hierarchy"
domain: "brand, documentation, proposal-writing"
confidence: "high"
source: "earned — README restructure proposal 2026-04-05"
---

## Context

When a proposal requires input from multiple researchers with different lenses (brand/copy, PM/adoption, UX/IA), the outputs need to be synthesized into a single actionable artifact rather than presented as parallel recommendations. This skill applies when:

- Two or more research passes have been completed independently
- The researchers have different authority levels (one may hold veto power)
- The final artifact is a proposal for review, not implementation

## Patterns

### 1. Hard constraints come first

If any researcher has explicit non-negotiables (UX requirements, accessibility mandates, brand locks), surface them at the top of the proposal as a numbered table. This prevents the table-stakes requirements from being buried in rationale sections where they can be overlooked.

**Format:**
```
| # | Constraint | Source |
|---|-----------|--------|
| 1 | [constraint text] | [researcher §section] |
```

### 2. Map agreements before surfacing conflicts

Identify where all researchers agree before calling out where they diverge. Agreement zones become the uncontested structure; divergence zones become the explicit decision points. This focuses team review time on actual decisions, not re-litigating consensus.

### 3. Resolve conflicts using authority, not averaging

When Researcher A recommends X and Researcher B recommends Y, don't split the difference. Apply the authority hierarchy:
- Hard constraints (Elaine/UX accessibility, brand locks) override copy/PM recommendations
- Where authority is equal, apply the stronger rationale with explicit reasoning
- Where genuinely unresolvable, surface as an open item requiring the principal's decision

### 4. Rationale for each section, not just the overall structure

For each proposed section, document: (a) what the content is, (b) which research inputs support it, (c) what alternatives were rejected and why. This makes the proposal reviewable — team members can challenge the rationale without reopening settled questions.

### 5. Defer, don't block

Proposals often have open items (final hero sample, URL placeholders, color roles pending another pass). Document them explicitly as "what this proposal does not decide" — this prevents the proposal from being blocked on inputs that don't exist yet, while ensuring nothing is forgotten.

### 6. Dual-audience architecture (human + AI)

For developer-facing documentation, every structural decision should be validated against both human reader patterns (F-pattern, cognitive load, mobile viewport) and AI reader patterns (semantic structure, language tags, alt text). A table mapping "need → structural choice" for each audience makes this explicit and reviewable.

## Examples

**File:** `brand/references/readme-restructure-proposal.md`

Key structural pattern: 16 hard constraints in a table at the top, then section-by-section rationale, then explicit list of what the proposal does not decide.

**Researcher authority hierarchy used:**
1. Elaine (UX/IA) — hard constraints, non-negotiable
2. Brand decisions (locked) — positioning, voice, color
3. Peterman (brand/copy) — structure and copy recommendations
4. Steinbrenner (PM) — evaluation journey and adoption flow

### 9. Pre-rewrite completeness check — verify review changes are already incorporated

Before executing a rewrite, cross-check all reviewer required-change lists against the most recent proposal revision date. Proposals often go through a revision pass between initial review and execution. If the revision post-dates the reviews, map each required change against the revised document before scheduling a new patch pass.

**Pattern:** Read the proposal's revision date. If it post-dates the reviews, scan for each required change by keyword. If all are present, mark as incorporated and proceed to draft. A spurious pre-rewrite patch pass creates churn and delays the actual work.

**Example:** The README restructure proposal was reviewed on 2026-04-06 (Frank, George, Uncle Leo — 9 required changes across the three reviews). The proposal was revised on 2026-04-07. Cross-check showed all 9 were already addressed. No patch pass was needed.

### 10. Identify the single blocking decision before staging a multi-section draft

When a multi-section document has one section with a genuine undecided dependency (e.g., a hero domain selection that requires the principal's judgment) and all other sections are fully spec'd, the correct move is:

1. Name the blocking decision explicitly — do not let it block the entire rewrite
2. Draft all non-dependent sections immediately
3. Leave a [PLACEHOLDER] in the blocked section
4. Queue the blocked section for a second-round fill-in after the decision lands

**Example:** README §2 Quick Example depends on hero domain (Shane's call). Sections §0, §1, §3–§7 are fully spec'd and can be drafted without it. Two-round approach: draft §0+§1+§3-§7 now, fill §2 after Shane names the domain. This collapses a multi-session project into a two-round task.



- **Averaging divergent recommendations** — produces neither researcher's intent and satisfies no constraint
- **Burying constraints in rationale sections** — constraints get treated as suggestions when not surfaced pronounced
- **Open items masquerading as decisions** — marking something as decided when it requires a principal's call creates false confidence
- **Solving for one audience only** — documentation that serves human readers but not AI readers (or vice versa) is incomplete for an AI-native product
- **Writing the proposal as if it's the implementation** — a proposal is a structure recommendation, not draft copy; the two should not be conflated
- **Full deferral of "why this approach"** — pushing all philosophical rationale to docs leaves the README as a feature list; the *core* diagnosis (why the problem is real, why this approach prevents it) belongs in the README itself; only the deep construct mechanics defer
- **Cutting a phrase because it's redundant as a standalone** — before cutting, check whether it can be *folded* into an existing sentence as a mechanism phrase; redundancy as a standalone sentence ≠ redundancy as a participial clause

## Patterns (continued)

### 7. Integrating principal feedback without reopening settled questions

When a principal provides targeted feedback on an approved proposal (e.g., "retain this phrase," "keep this section"), the revision should:

1. **Apply the feedback surgically** — change only what the feedback targets; do not revisit settled structure
2. **Document the feedback rationale in-place** — update the section-level rationale to explain *why* the change was made, so reviewers can follow the reasoning without needing the original feedback message
3. **Update the trim summary** — the trim summary is the cost ledger; any feedback that changes what was cut/deferred must update that table
4. **Preserve prior decision authority** — team reviews (Frank, George, Elaine) were made against the prior version; feedback from the principal overrides where there is conflict, but does not re-litigate what the team agreed on
5. **Write the decision to inbox** — if the feedback establishes a new named decision (a phrase form, a section rule, a defined term), it belongs in the decisions inbox

### 8. The "fold, not append" principle for brand phrases

When a phrase is valuable but redundant as a standalone sentence, try folding it as a participial clause before cutting. The test: does it add mechanism (how) or modifier (what kind) to the sentence it joins? If yes, fold. If it merely restates the same claim in different words, cut.

**Example:** "By treating your business constraints as unbreakable precepts" is redundant as a third sentence after "invalid states are structurally impossible" — both describe the same outcome. But as a participial lead-in to the positioning sentence, it adds the *mechanism* (treating-as-unbreakable) before the *outcome* (structurally impossible). That fold earns both phrases in one sentence.

