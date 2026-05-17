# Spec Construction Idioms — Durable Naming

**Author:** Frank (Lead/Architect)  
**Date:** 2026-05-17  
**Requested by:** Shane  
**Status:** Decision Record

---

## Decision

`docs/language/precept-language-spec.md §3A.5` should treat Precept construction as two named, complementary first-class idioms:

1. **Constructor with existential fields**
2. **Free construction + governed draft state**

The spec should not present Pattern B as a fallback or loophole. It is a governed lifecycle shape in which the entity is valid from birth under draft-appropriate truth, then crosses an activation gate (`Publish`, `Submit`, etc.) into stricter invariants.

---

## Rationale

The mechanics in §3A.5 were already correct, but unnamed mechanics leave an avoidable design gap for readers and downstream AI/tooling consumers. The actual design choice is domain-driven:

- **Existential requirements at birth** → constructor with existential fields
- **Legitimate progressive enrichment phase** → free construction + governed draft state

This naming makes the core distinction durable without changing language surface or semantics.

---

## Implications

- `LoanApplication` is the natural reference shape for **Constructor with existential fields** when applicant identity and requested amount are constitutive at intake.
- `samples/inventory-item.precept` remains the canonical **Free construction + governed draft state** example.
- Future docs, examples, and pattern guidance should use these names consistently when explaining construction choices.
- Wording about Pattern B should emphasize **lifecycle-appropriate governance**, not "ungoverned draft" semantics.
