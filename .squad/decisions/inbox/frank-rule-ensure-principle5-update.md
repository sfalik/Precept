# Decision: Update Design Principle #5 with rule/ensure vocabulary

**Date:** 2026-04-15  
**Author:** Frank (Lead/Architect & Language Designer)  
**Owner approval:** Shane — approved 2026-04-15  

## Decision

Design Principle #5 in `docs/PreceptLanguageDesign.md` will be updated to reference the new `rule`/`ensure` keywords when the feature is implemented (issue #96).

**Current wording:**

> **Data truth vs movement truth.** `invariant` = static data constraints (always hold). `assert` = movement constraints (checked when something happens — an event fires, a state is entered or exited). The keyword tells you the category.

**New wording (to be applied in the implementation PR):**

> **Data truth vs movement truth.** `rule` = static data constraints (always hold). `ensure` = movement constraints (checked when something happens — an event fires, a state is entered or exited). The keyword tells you the category.

## Why this is a philosophy-level change

Principle #5 is not just documentation — it is a design principle that governs how constraint keywords are chosen and evaluated. Changing the specific keywords referenced in a design principle is a philosophy-level edit, not a routine docs update. It requires explicit owner sign-off.

## Why it's approved

The semantic distinction (data-truth vs movement-truth) is preserved exactly. Only the vocabulary changes — from formal verification jargon (`invariant`/`assert`) to domain-expert-accessible language (`rule`/`ensure`). The principle's function as a design constraint is unchanged; it now uses words that match the DSL's identity as a readable contract language.

## Timing

The update is applied in the same PR that implements issue #96, per the established convention that `PreceptLanguageDesign.md` tracks what EXISTS in the runtime, not what is PLANNED.
