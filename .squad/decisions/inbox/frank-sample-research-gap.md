# Decision: Enterprise Platform Research & Sample Corpus Gaps

**Author:** Frank
**Date:** 2026-07-18
**Status:** Decided
**Artifact:** `docs/research/sample-realism/frank-enterprise-platform-and-research-gaps.md`

## Context

Shane asked whether additional research beyond what we'd already done could improve the sample set. I surveyed eight enterprise platforms (Salesforce, ServiceNow, Pega, Appian, Camunda/BPMN/DMN, IBM ODM/BAW, Guidewire, Temporal) plus adjacent standards (CMMN, DDD aggregates, ACORD).

## Decisions

1. **Four missing lifecycle shapes identified.** The authorization-gate (Change Request), appeal-loop (Benefits Application), containment-phase (Security Incident), and amendment-loop (Insurance Policy) patterns are genuine structural gaps — not domain-name gaps. They recur across 3+ enterprise platforms independently. The next sample wave must fill these.

2. **Three research lanes should precede the next major sample authoring pass:** (a) domain expert interview protocols, (b) regulatory requirement mining, (c) state-graph shape taxonomy. Writing more samples without this research risks producing more domain-shallow files.

3. **The platform survey is a living reference.** The eight lifecycle shapes and ten policy patterns tables should be updated as the team encounters new patterns.

4. **Eight concrete new-sample candidates are ranked by evidence strength** — change-request, benefits-application, insurance-policy-lifecycle, security-incident, customer-onboarding-kyc, invoice-lifecycle, building-permit, problem-investigation. Implementation priority depends on which language features ship when.

## Impact

- Extends the sample-realism research with enterprise-ecosystem evidence
- Provides 8 ranked research lanes for continued improvement
- Does NOT change the 42-sample ceiling or phased plan — adds evidence for what should fill remaining slots
