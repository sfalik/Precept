---
name: "proposal-as-skeleton"
description: "A sufficiently detailed proposal IS the structural spec — a separate skeleton document adds an extra pass without adding structural coverage when the proposal already specifies headings, constraints, and per-section format"
domain: "documentation, proposal-writing, design review, collaboration sequencing"
confidence: "high"
source: "earned — README shape-first evaluation 2026-04-08 (Elaine)"
---

# Skill: Proposal as Skeleton — When to Skip the Pre-Draft Scaffold

## The Problem

A structural role (UX, design, IA) issues hard constraints for an artifact. The constraints are incorporated into a detailed proposal. Someone asks: "Should the structural role produce a skeleton document before the writer drafts?"

The instinct is yes — constraints are clearer when embedded in a file that looks like the artifact. But if the proposal is already detailed enough, a skeleton pass creates two sources of structural truth, adds an extra round trip, and produces an artifact the executor must interpret alongside the proposal.

## The Decision Rule

**Ask: is the proposal abstract or specific?**

| Proposal type | Skeleton pass value |
|---|---|
| Abstract — section names and intent only | High — skeleton translates intent to form |
| Specific — heading levels, per-section content guidance, format rules | Low — skeleton reproduces what already exists |
| Highly specific — template copy, viewport constraints, CTA hierarchy, annotated examples | Negligible — the proposal IS the skeleton |

A skeleton pass earns its cost when the proposal leaves decisions the executor will make independently. When the proposal has already made those decisions explicitly, the skeleton is a mechanical reproduction step.

## The Exception: Hero/Key Block Annotation

Even when a proposal is highly specific, there is one scenario where a targeted pre-draft artifact adds value the proposal cannot:

**When a constraint shapes the physical form of a key content block in ways that prose rules don't make visible.**

Example: "Hero code block: ≤20 lines, ≤60 chars per line." This rule is clear in prose. But a writer who has never seen what 60-character-wide DSL looks like may not know what field names, guard expressions, or rule text need to be abbreviated to fit. A stub with inline character-count markers — not full copy, just structural annotation — shows the constraint in its context, not as a rule in a separate document.

The targeted artifact for this is NOT a full skeleton. It is a single annotated block:

```
<!-- ≤20 lines | ≤60 chars per line | viewport constraint #8 -->
<!-- Required constructs: precept decl, 2-3 fields, 3 states (initial marked), 2 events, 1 guard, 1 invariant -->
precept [Domain]
  [field: type]
  ...
```

## Inline Constraint Annotation

When a scaffold IS produced (because the proposal is abstract), the mechanism that earns its keep is **HTML comment annotations co-located with constrained content slots**. A comment directly above a constrained slot is harder to accidentally bypass than a rule in a document two files away.

Pattern:
```markdown
<!-- Constraint #1: logo + hook + primary CTA visible within 550px vertical -->
<!-- Badge row: 3 badges max — NuGet version, License, Build — in that order -->
![NuGet](badge) ![License](badge) ![Build](badge)

<!-- Constraint #8: hero block ≤20 lines, ≤60 chars per line -->
```precept
[stub]
` ` `
```

The annotation names the constraint, links to its number, and is positioned directly before the slot it governs. An executor who ignores it has actively deleted a warning, not accidentally missed a rule.

## Two Sources of Structural Truth Is One Too Many

If a scaffold file and a proposal both specify heading structure and they disagree at any point, the executor must adjudicate. This creates friction at the worst time — during active drafting.

**One source of structural truth per phase:**
- Planning phase: the proposal
- Drafting phase: the scaffold (if one exists) — or the proposal if no scaffold
- Audit phase: the delivered artifact, checked against the constraint table

Never have both a proposal and a scaffold active simultaneously unless the scaffold explicitly supersedes the proposal sections it covers.

## Application

```
1. Read the proposal with an executor's eye: what decisions does it leave open?
2. If the open decisions are format/form decisions (heading levels, code block shape, CTA hierarchy) → proposal is abstract → scaffold adds value
3. If the open decisions are content decisions (which domain, what words, which bullets) → proposal is specific enough → skip the scaffold
4. If there's a single key block (hero, diagram, data table) with physical-form constraints → produce a targeted annotated stub for that block only
5. Lock the content decisions (hero domain, key choices) as Gate-Before-Start items (see: proposal-gate-analysis skill) before either the scaffold or the copy is written
```

## Related Skills

- `constraint-holder-review-gate` — The constraint-holder's role is post-draft audit, not pre-draft skeleton production
- `proposal-gate-analysis` — What the PM must lock before execution begins (hero domain is always a gate)
