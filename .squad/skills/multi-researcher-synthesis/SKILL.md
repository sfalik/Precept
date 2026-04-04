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

## Anti-Patterns

- **Averaging divergent recommendations** — produces neither researcher's intent and satisfies no constraint
- **Burying constraints in rationale sections** — constraints get treated as suggestions when not surfaced prominently
- **Open items masquerading as decisions** — marking something as decided when it requires a principal's call creates false confidence
- **Solving for one audience only** — documentation that serves human readers but not AI readers (or vice versa) is incomplete for an AI-native product
- **Writing the proposal as if it's the implementation** — a proposal is a structure recommendation, not draft copy; the two should not be conflated
