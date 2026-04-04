---
name: "constraint-holder-review-gate"
description: "When a researcher issues hard constraints for an artifact, they must also serve as the post-draft compliance reviewer — not just a pre-draft contributor"
domain: "documentation, proposal-writing, design review"
confidence: "high"
source: "earned — README rewrite role definition 2026-04-07"
---

# Skill: Constraint-Holder as Post-Draft Gate Reviewer

## The Problem

Hard constraints issued during a research phase are treated as governing requirements. They get listed in a proposal. Reviewers acknowledge them. The executor says "we'll satisfy these." Then the draft is written and nobody checks.

The constraint-holder — the person who issued the requirements — is most qualified to verify whether the delivered artifact satisfies them. If they don't serve as the post-draft gate, the constraints were advisory, not hard.

## The Pattern

**The constraint-holder must review the post-draft artifact.** This is the closed loop.

```
Research → Constraints issued (constraint-holder)
         ↓
         Proposal → Constraints baked in (executor)
         ↓
         Draft → Constraints checked (constraint-holder again)
         ↓
         Compliance report → Executor resolves gaps
         ↓
         Principal sign-off
```

The constraint-holder does not write the draft. They do not collaborate on copy. They run a pass/fail audit: for each constraint, did the delivered artifact satisfy it?

## When to Apply

- When a proposal was gated by hard constraints from a specific role (UX, accessibility, brand)
- When the executor (writer, implementer) is different from the constraint-holder
- When the artifact will be published or shipped (not just internal tooling)

## How to Structure the Audit

1. **Read the constraint table** (should be at the top of the proposal)
2. **Check each constraint against the delivered artifact** — not against intent, against the actual content
3. **File a pass/fail report** — one row per constraint
4. **Flag failures clearly** — "Constraint #4 violated: H2 → H4 skip in What Makes Precept Different section"
5. **Do not rewrite** — the constraint-holder's job is diagnosis, not remediation

## Separation of Concerns

The audit is structural, not editorial. The constraint-holder should not be editing copy, refining tone, or rewriting sections. That conflates two different roles and produces a worse result for both.

| Constraint-Holder Reviews | Executor Owns |
|--------------------------|---------------|
| Heading hierarchy compliance | Copy quality and voice |
| CTA structure (primary/secondary) | Positioning language and phrasing |
| Viewport resilience | Section content decisions |
| Progressive disclosure order | Hero sample selection |
| AI/human readability mechanics | Link anchor text content |
| Line-length compliance in code blocks | Badge selection |

## Why This Works

- The constraint-holder knows their constraints best. They will catch violations that a second reviewer would miss.
- The executor can write freely without trying to self-audit against a list they didn't author.
- The principal gets a clean sign-off surface: if both the executor and the constraint-holder cleared it, the artifact is ready.

## Anti-Patterns

- **Constraint-holder writes the draft** — conflates research with production; produces copy constrained by UX concerns that should be structurally resolved, not compromised in
- **Constraint audit skipped** — the most common failure; constraints live only in the proposal, not in the artifact
- **Constraint-holder provides line edits** — correct domain (UX) but wrong mode (editorial); flags should name the violation, not provide the fix
- **Constraint-holder is replaced by a general reviewer** — general reviewers won't know which violations are hard (heading skips) vs. soft (tone preferences)

## Examples

**README rewrite (Precept, 2026-04-07):**
- Elaine issued 16 hard UX constraints in `brand/references/readme-research-elaine.md`
- Peterman synthesized the proposal, treating them as non-negotiables
- Correct gate sequence: Peterman drafts → Elaine audits compliance → Peterman resolves → Shane signs off
- Elaine does not write copy; Peterman does not audit viewport resilience

## Related Skills

- `proposal-gate-analysis` — What the PM must lock before execution begins (pre-draft)
- `multi-researcher-synthesis` — How to synthesize multiple research passes into a single proposal with constraint hierarchy
