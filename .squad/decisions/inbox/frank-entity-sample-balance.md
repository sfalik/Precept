### 2026-05-19: Entity/data-contract sample balance correction
**By:** Frank (Lead Architect & Language Designer)
**What:** Corrected the sample-realism research framing from workflow-only to entity-inclusive. Key decisions:

1. **Reference data promoted from Tier 3 to Tier 2** in domain-fit ranking. Entity/reference data entities are structurally prerequisite to Tier 1 workflow domains — they cannot be peripheral.
2. **Corpus target revised:** 60–70% workflow, 15–20% entity (stateless), 8–12% hybrid (lifecycle-light), 5–8% teaching. Prior framing was 100% workflow with data-only at ~10–15% and Teaching-only.
3. **Three precept archetypes defined:** Workflow, Entity (stateless), and Hybrid (lifecycle-light, field-heavy). Prior research only planned for one.
4. **Dilution test question #1 reframed:** "Does this entity require governed integrity — lifecycle transitions, field constraints, editability rules, or a combination?" replaces "Does this domain require governed lifecycle transitions?"
5. **Entity samples span the full complexity gradient:** Teaching (Customer Profile), Standard (Coverage Type, Provider), Complex (Fee Schedule, Policy Template). Not all data-only samples are entry ramps.
6. **Domain suite concept introduced:** At least two domains should show workflow + entity precepts together (e.g., insurance-claim + insurance-adjuster + insurance-coverage-type).
7. **Six new research lanes identified:** FluentValidation pattern catalog, industry data standards survey, Salesforce object model deep-dive, JSON Schema constraint comparison, master data entity shapes, decision-table-to-invariant mapping.

**Why:** Shane's directive that Precept is for modeling business entities, not just workflow. Issue #22 (data-only precepts) makes this explicit at the language level. The prior research framed data-only as a footnote; this correction treats it as a core product capability.
**Artifact:** `docs/research/sample-realism/frank-entity-modeling-addendum.md`
**Team impact:** Steinbrenner's portfolio plan, Peterman's domain benchmarks, and George's sample audit should incorporate the entity/hybrid archetype when planning the next expansion pass.
