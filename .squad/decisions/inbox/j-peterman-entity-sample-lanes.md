# Decision: Entity-centric sample lanes for stateless precepts

**Filed by:** J. Peterman (Brand/DevRel)  
**Date:** 2026-05-17  
**Status:** Research delivered — awaiting team review

## What

External benchmarking across 15 entity-modeling ecosystems (Salesforce, ServiceNow, SAP MDG, Guidewire, JSON Schema, Zod, FluentValidation, Pydantic, Drools, NRules, XState, Terraform, OpenAPI, DDD literature, ERP master data patterns) shows that if stateless/data-only precepts land (#22), the sample corpus should add **three explicit non-workflow lanes**:

1. **Master data contracts** (2-3 samples): Vendor, Product/Material, Employee — mutable business entities with cross-field constraints, enumerated vocabularies, and controlled editability.
2. **Reference data definitions** (1-2 samples): RiskTier, PaymentTerms — compact, mostly-locked lookup entities.
3. **Domain-rule contracts** (1 sample): CreditApplication or similar — where invariants encode classification/eligibility policy, not just field validation.

**Total: 4-6 new stateless samples**, fitting within the existing ceiling decision (30-36 canonical, 42 hard upper bound).

## Why

- Every enterprise platform studied supports both stateful and stateless entity modeling in the same language. Precept would be joining an established pattern.
- The strongest entity samples are **master data entities** with cross-field constraints and business-facing rejection messages — not flat schemas with single-field range checks.
- The differentiation against Zod/FluentValidation/JSON Schema is that Precept invariants hold across all mutations (not just at input time) and are inspectable. Samples must demonstrate this.
- Credibility for entity samples requires: domain-bearing field names, at least one cross-field constraint, at least one enumerated vocabulary, realistic nullability, and business-toned because messages.

## Artifact

`docs/research/sample-realism/peterman-entity-centric-benchmarks.md`

## Team implications

- **George (sample authoring):** When stateless precepts ship, entity samples should lead with Vendor or Product — universally understood, constraint-dense, and well-grounded in external precedent.
- **Frank (language design):** The entity lane reinforces the value of `choice` type (#25) and field-level constraints (#13) — both are prerequisite for credible stateless samples.
- **Steinbrenner (planning):** 4-6 stateless slots need to be reserved in the sample budget when #22 lands. They replace no existing workflow samples.
