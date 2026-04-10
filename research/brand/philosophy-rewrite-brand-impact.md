# Philosophy Rewrite — Brand Impact Assessment

**Author:** J. Peterman, Brand/DevRel  
**Requested by:** Shane  
**Scope:** Impact on locked brand positioning if Precept shifts from state-first framing to **entity-first, data-centric** framing  
**Inputs read:**  
- `design/brand/research/data-vs-state-philosophy.md`  
- `design/brand/research/data-vs-state-architecture.md`  
- `design/brand/research/data-vs-state-runtime.md`  
- `docs/references/data-vs-state-pm-research.md`  
- `.squad/decisions/inbox/copilot-directive-2026-04-08T00-51-03Z.md`  
- `.squad/decisions/inbox/copilot-directive-2026-04-08T01-30-30Z.md`  
- `design/brand/philosophy.md`  
- `design/brand/brand-decisions.md`

---

## Read-first note on the directives

The **entity, not workflow** directive is present in `copilot-directive-2026-04-08T01-30-30Z.md`.

The referenced `copilot-directive-2026-04-08T00-51-03Z.md` currently contains a separate note about README energy and sizzle, not the data-emphasis directive described in Shane's brief. For this assessment, I am treating **Shane's request in this brief** as the authoritative source for the data-emphasis decision:

> Data and rules are at the heart of Precept. States are not the headline; they are the structure through which entity data moves and is governed.

That aligns with the research findings even if the inbox item has not yet been synchronized.

---

## Executive judgment

This is **not** a category change. It is a **philosophical correction and emphasis shift** inside the existing category.

Precept should still present itself as a **domain integrity engine**. What changes is the brand's explanation of **what integrity applies to**:

- not primarily state-machine correctness,
- but the integrity of a **business entity's data and rules across its lifecycle**.

The research converges on the same conclusion:

- **Peterman:** the current line is technically right but mechanism-forward; the brand should lead with the outcome developers care about: valid entity data.  
- **Frank:** architecturally, state is essential but not primary in the brand sense; the real guarantee is about the joint `(state, data)` configuration.  
- **George:** at runtime, all constraint expressions are data expressions; state activates rule sets, but the engine protects configurations.  
- **Steinbrenner:** pure data-first is too broad and risks the validation-library trap, but entity/data-first framing is stronger if lifecycle remains visible as the governing structure.

So the right move is:

> **Entity first. Data and rules at the center. Lifecycle as the governing structure. Workflow as one dimension, not the identity.**

---

## 1. What changes in `brand-decisions.md`

The locked document does **not** need a wholesale rewrite. It needs a **surgical repositioning update** in the places where the current language still centers state as the headline concept.

### A. Update the combined single-sentence positioning

**Current locked sentence**

> "Precept is a domain integrity engine for .NET — a single declarative contract that binds state, data, and business rules into an engine where invalid states are structurally impossible."

**Why it now falls short**

- It lists **state** first, which implies mechanism before subject.
- It frames the guarantee as **invalid states**, which both Frank and George show is incomplete.
- It does not say clearly enough that Precept is for **modelling business entities**.
- It leaves room for the old workflow-engine mental model.

**Brand decision update needed**

Lock a new primary sentence that:
- leads with **business entities / entity data**,
- keeps **domain integrity engine** as the category anchor,
- replaces **invalid states** with **invalid configurations**,
- preserves **structurally impossible**.

### B. Add an explicit positioning note: Precept models entities, not workflows

This is the biggest philosophical clarification Shane introduced.

`brand-decisions.md` should explicitly say:

- Precept is **not** primarily a workflow engine.
- Workflow/lifecycle is one dimension of a governed entity.
- The core object is the **business entity** and its **rules**.

Without this statement, future copy will keep drifting back toward workflow framing because the product historically evolved from that space.

### C. Update the "Do / Don't" examples under Voice

One of the current approved examples is:

> "Invalid states are structurally impossible."

That example should be updated to:

> "Invalid configurations are structurally impossible."

This matters because the voice section is where phrasing becomes normalized. If the example remains state-first, the old philosophy will keep reappearing in README copy, site copy, and demos.

### D. Update the README narrative guidance inside the brand document

The narrative arc itself can stay the same, but the **second sentence guidance** should shift from mechanism framing to entity framing.

The supporting difference sentence should teach:

- Precept governs how **entity data evolves under rules**,
- across a lifecycle,
- in one contract.

It should not open by sounding like a state-machine framework or workflow platform.

### E. Reorder conceptual priority where lists imply philosophy

Anywhere the brand document lists concepts in the order:

> state, data, rules

it should move to:

> entity, data, rules, lifecycle

or, when speaking at the contract level:

> data, rules, lifecycle

This is subtle, but order communicates hierarchy.

---

## 2. Flagship positioning sentence

## Recommended revision

> **Precept is a domain integrity engine for .NET — a single declarative contract for modelling business entities, governing how their data evolves under business rules across a lifecycle and making invalid configurations structurally impossible.**

### Why this is the right sentence

- **"domain integrity engine" stays** — the category is still correct.
- **"for modelling business entities"** answers Shane's new directive directly.
- **"their data evolves under business rules"** puts the governed thing first.
- **"across a lifecycle"** keeps lifecycle visible without reducing Precept to workflow.
- **"invalid configurations"** is more precise than "invalid states."
- **"structurally impossible"** preserves the prevention claim that differentiates Precept.

### Shorter alternate, if we need more compression later

> **Precept is a domain integrity engine for .NET — a single declarative contract that governs a business entity's data and rules across its lifecycle, making invalid configurations structurally impossible.**

This alternate is slightly less explicit about "modelling," but cleaner if the flagship line needs to run tighter in a README hero or package description.

---

## 3. What stays the same

Several locked decisions should remain unchanged.

### Unchanged: category anchor

**Domain integrity engine** still holds. In fact, the new philosophy completes it. "Integrity" naturally maps more cleanly to governed entity data and rules than to state-machine identity alone.

### Unchanged: narrative archetype

Precept is still a **Category Creator** brand. Nothing in this shift moves it into "better validator," "better state machine," or "better workflow engine" territory. The category remains the same; the explanatory center of gravity changes.

### Unchanged: AI-native secondary frame

The AI story still belongs in supporting copy. If anything, entity/data framing may improve it, because "business entity contract" is even more legible to AI-assisted authoring than "state machine semantics," but the placement stays the same.

### Unchanged: voice baseline

The voice should remain:

- authoritative,
- precise,
- explanatory when needed,
- free of hype,
- factual about guarantees.

This change does not justify warmer, splashier, or more "solution marketing" language.

### Unchanged: visual language system

The design system already supports this philosophy well:

- data already has its own semantic lane,
- states remain visible but not exclusive,
- the DSL remains the hero surface,
- diagrams remain valid as a secondary surface.

No brand-color, typography, or visual-system reset is warranted by this shift.

### Unchanged: core supporting claims

These still hold and should remain prominent:

- **Prevention, not detection**
- **One file. Every rule.**
- **Same definition + same data = same outcome**
- **Full inspectability**
- **Compile-time checking**

Those are not state-first claims; they survive the rewrite intact.

---

## 4. Voice implications

The tone does **not** change. The emphasis does.

### What stays true

We should still sound like a technical product with hard edges:

- declarative,
- exact,
- deterministic,
- governed,
- inspectable.

### What should change in phrasing habits

We should use **entity language more often than workflow language** in foundational copy.

Prefer:

- **business entity**
- **entity data**
- **lifecycle**
- **configuration**
- **rules / invariants / constraints**

Use more carefully:

- **workflow**
- **state machine**
- **process**

Those words are still valid, but they should move down from identity language into mechanism or explanation language.

### Practical voice implication

The new philosophy calls for **less graph-theory shorthand** and **more domain-model shorthand**.

That means copy should sound closer to:

> "This entity cannot enter a configuration that violates the contract."

than:

> "This state machine cannot reach an invalid node."

The first is brand-aligned after the rewrite. The second is technically intelligible but too mechanism-led for the new philosophy.

---

## 5. Risk check

This shift is directionally right, but it introduces real positioning risks if we get sloppy.

### Risk 1: sounding like an ORM or Entity Framework companion

If we say "entity modelling" without enough rule/governance language, some developers will hear:

> data model, persistence model, ORM mapping

That would be wrong.

**Mitigation:** always pair entity language with **rules, lifecycle, contract, invariants, or integrity**.  
Never let "entity modelling" stand alone as if Precept were a schema mapper.

### Risk 2: sounding like a FluentValidation alternative

If we overcorrect into pure data language, we fall into the validation-library trap Steinbrenner warned about.

**Mitigation:** keep lifecycle/configuration visible in the flagship claim.  
Precept is not "validation for POCOs." It governs how entity data remains valid **through change**.

### Risk 3: erasing state so aggressively that we undersell the mechanism

Frank and George are both clear: state is not cosmetic. It activates constraints, routes transitions, and governs editability.

**Mitigation:** do not use passive metaphors like "state is just a vehicle" in flagship copy.  
The copy can make data primary without pretending state is trivial.

### Risk 4: drifting back into workflow-engine language

Because Precept evolved from workflow thinking, this is the easiest regression.

**Mitigation:** in top-level copy, always ask:

- Is the hero noun the **entity**, or the **workflow**?
- Is the promise about **integrity**, or about **process orchestration**?

If the answer trends toward orchestration, the copy is off course.

---

## Final recommendation

Update the brand philosophy and locked positioning to reflect this hierarchy:

1. **Precept models business entities.**
2. **Data and rules are the heart of the contract.**
3. **Lifecycle is the governing structure for that entity.**
4. **Workflow is one expression of lifecycle, not the product's identity.**
5. **The guarantee is about invalid configurations, not invalid states alone.**

In brand terms, this is a refinement, not a reinvention. It makes Precept sound more like what it has become.
