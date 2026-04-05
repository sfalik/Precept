---
name: "dsl-philosophy-filter"
description: "Evaluate DSL compactness proposals against Precept's core philosophy before recommending them"
domain: "language design, proposal review, product philosophy"
confidence: "high"
source: "earned — named guard philosophy pass 2026-04-05 (J. Peterman)"
---

# Skill: DSL Philosophy Filter

## Pattern

When a proposal makes the DSL shorter or more expressive, do not judge it on brevity alone. Test whether it makes Precept **more itself**.

## The Filter

Ask seven questions:

1. **Domain integrity:** Does the feature help keep invalid states structurally impossible, or does it push enforcement later?
2. **Determinism:** Does it preserve predictable, inspectable behavior?
3. **Keyword clarity:** Does the surrounding keyword still tell the reader what kind of rule they are looking at?
4. **Truth boundaries:** Does it preserve the distinction between data truth (`invariant`) and movement truth (`assert`)?
5. **Locality:** Does it reduce drift without hiding outcomes, actions, or enforcement moments?
6. **Compile-time soundness:** Can the checker validate it conservatively without inventing guesses?
7. **Alias creep / AI legibility:** Is it still a narrow domain construct that humans and agents can reason about, or is it quietly becoming a macro/value-alias system?

## Application

- If a feature compresses repetition while preserving those seven properties, it likely fits.
- If it saves lines by blurring rule categories, weakening compile-time guarantees, or creating hidden indirection, it likely does not.
- Prefer framing accepted features as named domain concepts, not convenience syntax.

## Example

Named rules pass the filter when they stay boolean-only, field-scoped, and reusable across `when`, `invariant`, and state `assert`.

They fail the filter if they expand into event-arg assertions, computed values, or rule-to-rule macro composition.
