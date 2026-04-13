# State-Graph Shape Taxonomy & Insurance Lifecycle Realism

**Author:** Frank (Lead Architect & Language Designer)
**Date:** 2026-07-19
**Status:** Research artifact — input for sample-realism initiative
**Depends on:** `frank-enterprise-platform-and-research-gaps.md` (Research Lane 3: State-Graph Shape Taxonomy), `frank-entity-modeling-addendum.md`, `frank-language-and-philosophy.md`

---

## Purpose

This document delivers two of the highest-priority research lanes from the enterprise platform survey:

1. **State-graph shape taxonomy** (Research Lane 3) — a formal classification of lifecycle graph shapes observed across governed enterprise lifecycles, mapped against the current 21-sample corpus to identify structural gaps.
2. **ACORD / insurance field-per-state realism** — a deep study of how real insurance lifecycles (ACORD-standard policy and claim models) govern field editability and data requirements at each lifecycle stage, and what this means for Precept sample credibility.

Both inform the same question: what structural patterns must the sample portfolio demonstrate to be credible for enterprise developers?

---

## 1. State-Graph Shape Taxonomy

### 1.1 The taxonomy

After surveying UML state machine literature, BPMN lifecycle specifications, enterprise platform models (ServiceNow, Guidewire, Pega, Appian, Camunda), and the 21 current samples, I identify **nine** recurring graph shapes in governed entity lifecycles. These are structural patterns — they recur across industries and platforms regardless of domain vocabulary.

| # | Shape | Description | Defining structural feature | Enterprise examples |
|---|-------|-------------|---------------------------|---------------------|
| **L** | **Linear** | States progress in one direction; no branches, no loops. | Max out-degree = 1 from each non-terminal state. Single decision point producing two terminal branches counts as linear-with-fork, the simplest branching variant. | Document approval (Draft → Review → Published), simple fulfillment |
| **B** | **Branching** | Paths diverge at decision points but never reconverge. Multiple terminal states. | Out-degree > 1 at decision nodes; no path from any divergence endpoint leads back to a shared successor. | Service request with cancel, repair with reject |
| **D** | **Diamond** | Paths diverge and reconverge before reaching terminal states. | Two or more paths from a decision node eventually merge at a shared downstream state. | Insurance claim (approve/deny paths both reach close), payment (authorize/capture reconverge) |
| **SL** | **Single-Loop** | One cycle in the graph — entity returns to a prior state. | Exactly one strongly connected component with >1 node. | Subscription retention (Active → Review → Active), simple retry |
| **ML** | **Multi-Loop** | Multiple distinct cycles, or a cycle reachable from more than one entry point. | Two or more cycles, or one cycle with multiple entry edges. | Library checkout (renew loop + overdue loop + return-to-shelf), complex retry with escalation |
| **AG** | **Authorization-Gate** | A dedicated approval/authorization state that blocks forward progress until an explicit authorization event occurs. The gate is structurally distinct from a simple guard condition — it is a state the entity occupies while waiting for authorization. | A state with no auto-transition, entered from a preparation state, exited only by an explicit authorize/reject event. | Change management (Assess → **Authorize** → Execute), procurement approval, regulatory gate |
| **AL** | **Appeal-Loop** | After a decision (approved/denied), the entity can loop back to a re-review state. The cycle includes the decision state. | A transition from a post-decision state back to a pre-decision review state, creating a cycle that includes the decision node. | Benefits application (Denied → Appeal → Re-review → Decision), insurance claim dispute, regulatory reconsideration |
| **EL** | **Endorsement/Amendment-Loop** | A long-lived "active" state from which the entity can cycle through a modification workflow and return to active. | A transition from an "active" or "in-force" state to a modification state, with a return path back to active. | Insurance policy (Active → Endorse → Active), contract amendment, policy mid-term change |
| **CP** | **Containment-Phase** | A distinct containment state sits between investigation and remediation. The entity must be explicitly contained before repair can begin. | A state between investigation and remediation that requires an explicit containment event; remediation is unreachable without it. | Security incident (Investigation → **Containment** → Remediation), safety incident, data breach |

**Composite shapes.** Real enterprise lifecycles often compose multiple shapes. An insurance claim is a Diamond (approve/deny reconverge at Close) with an Appeal-Loop (Denied → Appeal → Re-review). A security incident is a Linear core with a Containment-Phase inserted and potentially a Multi-Loop for investigation iteration. The taxonomy classifies the atomic shapes; real samples will be tagged with their composite.

### 1.2 Current corpus mapped to the taxonomy

| Shape | Current samples | Count | Coverage |
|-------|----------------|-------|----------|
| **L (Linear)** | apartment-rental-application, building-access-badge-request, loan-application, travel-reimbursement | 4 | ✅ Saturated |
| **B (Branching)** | clinic-appointment-scheduling, hiring-pipeline, maintenance-work-order, parcel-locker-pickup, refund-request, utility-outage-report, vehicle-service-appointment, warranty-repair-request | 8 | ✅ Over-represented |
| **D (Diamond)** | event-registration, insurance-claim | 2 | ⚠️ Under-represented |
| **SL (Single-Loop)** | crosswalk-signal, restaurant-waitlist, subscription-cancellation-retention | 3 | ✅ Adequate |
| **ML (Multi-Loop)** | library-book-checkout, library-hold-request, trafficlight | 3 | ⚠️ All toy/teaching domains |
| **AG (Authorization-Gate)** | — | 0 | ❌ **Missing** |
| **AL (Appeal-Loop)** | — | 0 | ❌ **Missing** |
| **EL (Endorsement/Amendment-Loop)** | — | 0 | ❌ **Missing** |
| **CP (Containment-Phase)** | — | 0 | ❌ **Missing** |

### 1.3 Structural diagnosis

The corpus has **twelve samples** in the two simplest shapes (Linear + Branching) and **zero** in the four enterprise-governance shapes that define real regulated lifecycles (AG, AL, EL, CP). This is the most damning structural finding possible.

A developer evaluating Precept for insurance, compliance, ITSM change management, or security operations will look at the samples and see: intake → review → approve/reject → done. That's a shape they've seen in every tutorial for every tool. The shapes that distinguish governed lifecycles from tutorial workflows — authorization gates, appeal loops, endorsement cycles, containment phases — are completely absent.

**The failure is not in domain count. It is in structural shape coverage.** Adding more samples in new domains that repeat the Linear/Branching shape would worsen the problem by increasing the illusion of breadth without adding structural depth.

### 1.4 Shape × domain gap matrix

This matrix shows which (shape, domain) cells are empty. Empty cells are genuine portfolio gaps.

| Shape | Insurance | Finance | ITSM | Healthcare | Compliance/Regulated | Security |
|-------|-----------|---------|------|------------|---------------------|----------|
| **L** | — | loan-app, travel-reimb | — | — | — | — |
| **B** | — | refund-request | maintenance-work-order, warranty-repair | clinic-appt | — | — |
| **D** | insurance-claim | — | — | — | — | — |
| **SL** | — | — | — | — | — | — |
| **ML** | — | — | it-helpdesk-ticket | — | — | — |
| **AG** | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **AL** | ❌ | ❌ | — | ❌ | ❌ | — |
| **EL** | ❌ | ❌ | — | — | ❌ | — |
| **CP** | — | — | — | — | — | ❌ |

The four enterprise shapes are entirely empty across all domains. This is the portfolio's structural deficit.

---

## 2. ACORD / Insurance Field-Per-State and Lifecycle Realism

### 2.1 Why insurance is the right depth-study domain

Insurance is Precept's Tier 1 domain. The existing `insurance-claim.precept` is the closest thing the corpus has to a flagship. ACORD provides the industry's canonical data and process model. Guidewire implements it. If Precept can credibly model an ACORD-aligned insurance lifecycle, the product story is proved. If it can't, no amount of helpdesk-ticket or library-checkout samples will compensate.

### 2.2 ACORD policy lifecycle — the endorsement-loop shape

The ACORD-standard policy lifecycle is a textbook Endorsement/Amendment-Loop:

```
Application → Quote → Bind → Issue → Active
                                       ↕
                                   Endorse → Active
                                       ↕
                                   Renew → Active
                                       ↓
                                   Cancel / Expire
```

**Key observations for Precept modeling:**

1. **Active is a long-lived state.** In the Linear/Branching samples we have today, no state lasts. Entities move through states and terminate. A policy's Active state can last years. This demands `edit` declarations for field mutability in Active — the entity lives there, and data changes happen there.

2. **Endorsement is a round-trip loop.** Active → Endorse → Active is the defining structural pattern. The entity leaves Active, enters a modification workflow, and returns to Active with changed field values. No current sample has this.

3. **Field editability shifts dramatically by state.** This is the critical finding for Precept:

| Lifecycle state | Editable fields | Locked fields | Why |
|----------------|----------------|---------------|-----|
| **Application** | All policyholder data, coverage selections, risk information | Policy number (system-assigned) | Data capture phase — everything is open |
| **Quote** | Coverage limits, deductibles, optional endorsements | Applicant core identity, risk location | Underwriter refines terms within risk profile |
| **Bind** | Payment method, effective date (within window) | Coverage structure, premium, risk data | Legal commitment — terms are frozen |
| **Issue** | None (read-only transition) | All | Document generation — no edits permitted |
| **Active** | Contact information, payment method, non-material changes | Policy number, inception date, original underwriting data, coverage structure (requires endorsement) | Routine maintenance only — material changes require endorsement workflow |
| **Endorse** | Fields affected by endorsement type only | All fields not in endorsement scope, policy number, inception date | Controlled change — only the specific modification is unlocked |
| **Renewal** | Updated risk data, new coverage options, premium | Original policy history, inception date | Renewal is a partial re-underwrite |
| **Cancel** | Cancellation reason, effective date | All other fields | Terminal transition |

**This is exactly what Precept's `edit` declarations model.** The state-scoped editability — "in Active, edit ContactPhone, ContactEmail, PaymentMethod" vs "in Endorse, edit CoverageLimit, Deductible, NamedInsured" — is Precept's native capability. But no sample demonstrates it at this level of domain realism.

4. **ACORD field immutability is structural.** Policy Number and Inception Date are immutable from issuance onward — not because of a business rule check, but because the contract's identity is fixed. In Precept terms, these are fields with no `edit` declaration in any post-issuance state. This is prevention, not detection.

5. **Regulatory fields vary by jurisdiction.** ACORD State Information Guides specify additional required fields per US state. A credible insurance sample should note this in comments: "FUTURE: jurisdiction-specific required disclosures would add conditional invariants per state regulatory code."

### 2.3 ACORD claim lifecycle — the diamond-with-appeal shape

The ACORD claim lifecycle combines Diamond (approve/deny reconverge at Close) with Appeal-Loop:

```
FNOL → Investigation → Evaluation → Settlement → Closed
                  ↕                       ↓
              Additional Info         Denial → Appeal → Re-review → Decision
                                                                     ↓
                                                                   Closed
```

**Key observations:**

1. **The existing `insurance-claim.precept` models the Diamond but not the Appeal-Loop.** It has Draft → Submitted → UnderReview → Approved/Denied → Paid. There is no appeal path. For an insurance flagship, this is a significant realism gap — claim appeals are a core insurance function.

2. **Reserve management is the missing field-governance story.** A real claim has reserve amounts (estimated future payout) that are set during Investigation, adjusted during Evaluation, and reconciled at Settlement. Reserve fields are editable in Investigation and Evaluation states but locked after Settlement. This is rich `edit` declaration territory.

3. **Authority levels gate settlement amounts.** An adjuster can settle claims up to their authority limit; above that, supervisor approval is required. This maps to Precept guards: `when SettlementAmount <= AdjusterAuthorityLimit`. The current sample has no authority-based guard.

4. **ACORD-aligned field vocabulary.** A credible insurance claim sample should use industry-standard field names:

| ACORD concept | Precept field | Type |
|--------------|--------------|------|
| Loss Date | DateOfLoss | number (FUTURE(#26): date) |
| Claim Amount | ClaimAmount | number (FUTURE(#27): decimal) |
| Reserve Amount | ReserveAmount | number |
| Deductible | PolicyDeductible | number |
| Coverage Type | CoverageType | string (FUTURE(#25): choice) |
| Adjuster | AssignedAdjuster | string |
| Settlement Amount | SettlementAmount | number |
| Denial Reason | DenialReason | string |
| Fraud Indicator | FraudIndicatorFlag | boolean |
| Required Documents | RequiredDocuments | set of string |

### 2.4 Guidewire implementation evidence

Guidewire's PolicyCenter and ClaimCenter implement the ACORD model with specific enforcement:

**PolicyCenter:**
- Policy transactions (New Business, Endorsement, Renewal, Cancellation, Reinstatement) are each a distinct lifecycle path through the policy entity.
- The Endorsement transaction creates a copy of the in-force policy, allows editing of specific fields, and upon approval replaces the active record. This is the Amendment-Loop in implementation.
- Underwriting rules fire at Quote and Bind transitions — guards on the transition that evaluate risk fields against eligibility criteria.
- Risk-based branching: policy type + geography + risk score → available coverages and required inspections.

**ClaimCenter:**
- FNOL creates a claim draft. Assignment routes to an adjuster based on claim type, severity, and geography.
- Investigation state supports iteration — the adjuster can request additional information, receive documents, and reassess. This is a self-loop within Investigation.
- Settlement authority is tiered: adjuster authority < supervisor authority < executive authority. The guard on the Settle event checks `SettlementAmount <= CurrentAuthorityLevel`.
- Subrogation and salvage are sub-lifecycles that branch from the main claim after settlement. These are out of scope for a single-entity Precept but worth noting in FUTURE comments.

### 2.5 What this means for the `insurance-claim.precept` rewrite

The current sample is structurally a Diamond with 6 states and 7 events. An ACORD-credible rewrite should be a Diamond-with-Appeal-Loop with:

| Dimension | Current | Target | Gap |
|-----------|---------|--------|-----|
| States | 6 | 8–10 (add Investigation iteration, Appeal, Reopened) | Missing appeal loop, missing investigation sub-states |
| Events | 7 | 10–12 (add RequestInfo, ReceiveInfo, Settle, Appeal, Reopen, CloseAppeal) | Missing evidence loop, appeal events |
| Guard complexity | Simple (DocumentCount > 0, ClaimAmount > 0) | Compound (authority limits, coverage verification, fraud checks, document completeness) | Shallow guards |
| Edit declarations | Not used | State-scoped: Investigation edits reserve; Settlement locks reserve; Active/Closed locks all | Missing editability governance |
| Invariants | Basic (Amount >= 0, non-empty strings) | Cross-field (SettlementAmount <= ReserveAmount, ClaimAmount > PolicyDeductible for non-zero settlement) | Missing domain-real constraints |
| Reject reasons | Generic | ACORD-aligned denial codes, specific regulatory reasons | Missing domain vocabulary |

---

## 3. How These Findings Reshape the Sample Portfolio

### 3.1 The structural-shape mandate

The portfolio must fill all four missing enterprise shapes (AG, AL, EL, CP) before adding more Linear/Branching samples. Each new shape sample must demonstrate a real enterprise domain — not an invented one.

**Recommended shape-filling samples:**

| Missing shape | Recommended sample | Domain | Why this domain |
|--------------|-------------------|--------|----------------|
| **AG (Authorization-Gate)** | `change-request.precept` | ITSM (ServiceNow-aligned) | Authorization gate is the defining feature of change management. Publicly documented in ServiceNow's Change State Model. Three variants (Normal, Standard, Emergency) show shape variation. |
| **AL (Appeal-Loop)** | `benefits-application.precept` | Regulated / public sector | Appeal loops define benefits and entitlement processing. Application → Eligibility → Decision → Appeal → Re-review → Final Decision. Strongest evidence from Appian public-sector and Pega healthcare. |
| **EL (Endorsement/Amendment-Loop)** | `insurance-policy-lifecycle.precept` | Insurance (ACORD-aligned) | The endorsement loop IS the insurance policy lifecycle. Active → Endorse → Active with field-editability shift. Strongest external evidence of any sample candidate. |
| **CP (Containment-Phase)** | `security-incident.precept` | Security operations (ServiceNow SecOps-aligned) | Containment as a distinct state between investigation and remediation is the defining feature of security incident response. Publicly documented. |

### 3.2 The insurance-depth mandate

Insurance is Precept's Tier 1 domain. The sample portfolio should include **three** insurance samples, not one:

1. **`insurance-claim.precept`** — Diamond-with-Appeal-Loop. The claim lifecycle with investigation iteration, settlement authority, appeal path, and ACORD-aligned field vocabulary. **Rewrite of existing.**
2. **`insurance-policy-lifecycle.precept`** — Endorsement-Loop. The policy lifecycle from application through active with endorsement, renewal, and cancellation. **New.** Demonstrates the long-lived-state and amendment-loop patterns.
3. **`insurance-adjuster.precept`** or **`insurance-coverage-type.precept`** — Entity/stateless. The reference-data entities that surround insurance workflows. **New.** Demonstrates that Precept governs the entire insurance domain, not just the workflow slice. FUTURE(#22) for stateless precepts.

Together, these form the **insurance domain suite** recommended in the entity-modeling addendum — workflow + entity precepts in the same domain.

### 3.3 Field-editability as a first-class sample concern

The ACORD research reveals that **state-scoped field editability is the most under-demonstrated Precept feature.** Current samples use `edit` declarations, but none demonstrate the pattern that makes enterprise developers sit up: fields that are editable in one state and locked in another, with the transition enforcing the lock.

Every flagship sample (insurance claim, policy lifecycle, change request, benefits application) should have:
- At least one field that is editable in early states and locked in later states
- At least one field that becomes editable only in a specific state (e.g., reserve amount editable only in Investigation)
- `edit` declarations that tell a visible governance story, not just a mechanical listing

### 3.4 Shape-tagging protocol

Every sample — current and new — should be tagged with its graph shape in the header comment:

```
# Shape: Diamond + Appeal-Loop
# Domain: Insurance (ACORD-aligned)
# Complexity: Complex
```

This enables mechanical portfolio analysis: `grep "# Shape:" samples/` produces a shape inventory. It also gives the team a shared vocabulary for discussing what's missing.

---

## 4. The Philosophy Directive

Shane's directive — now embedded in `docs/philosophy.md`, the entity-modeling addendum, and team decisions — has a clear current substance:

**Precept models business entities. States are optional. Workflow is only one dimension. Samples should not be watered down to current DSL limitations.**

This means:
1. The sample portfolio must demonstrate the full entity-modeling spectrum (workflow, entity, hybrid), not just workflow.
2. Samples model real domains at full complexity, using FUTURE(#N) comments where the language doesn't yet support the ideal expression. The domain is never simplified to match the current syntax.
3. Governed integrity — not just lifecycle transitions — is the governing concept. An entity that needs field constraints and editability rules but no states is a legitimate Precept use case.
4. The entity-modeling addendum's correction (from workflow-only framing to entity-inclusive framing) is not aspirational — it is the product's actual identity.

This directive is present in team memory through multiple channels:
- `docs/philosophy.md` §1 ("What the product does"), §2 ("The hierarchy of concepts"), §3 ("States are optional")
- `.squad/decisions.md` — the consolidated "Sample realism must cover workflows, entities, and stateless contracts" decision
- `docs/research/sample-realism/frank-entity-modeling-addendum.md` — the original correction document
- The entity-first positioning evidence in `docs/research/philosophy/`

---

## 5. Decisions

### 5.1 What I'm deciding

1. **The nine-shape taxonomy (L, B, D, SL, ML, AG, AL, EL, CP) is the structural vocabulary for portfolio planning.** Every sample should be classified by shape. Portfolio gaps are identified by empty cells in the shape × domain matrix, not by counting domain names.

2. **The four missing enterprise shapes (AG, AL, EL, CP) are the highest-priority structural gaps.** They outrank new domain additions. A portfolio with 30 samples in 12 domains but only Linear/Branching shapes is weaker than one with 25 samples in 8 domains that covers all nine shapes.

3. **Insurance must deepen to three samples (claim rewrite, policy lifecycle, reference entity).** This is driven by ACORD evidence, not preference. Insurance is the only domain where the external standard provides field-level, state-level lifecycle specifications detailed enough to benchmark against.

4. **State-scoped field editability must become a first-class sample design concern.** The ACORD field editability data proves that real enterprise lifecycles govern which fields can change at each stage. This is Precept's `edit` declaration in action, and the current samples under-demonstrate it.

5. **Shape tagging (in header comments) should be adopted as a corpus convention.** This is a mechanical, low-cost change that enables portfolio analysis and prevents the failure mode of adding structurally redundant samples.

### 5.2 What I'm NOT deciding

- Whether specific samples get written now vs later. That's a portfolio-planning decision.
- Whether the shape taxonomy gets encoded in tooling or metadata beyond sample comments. That's a tooling decision.
- The specific field names or guard conditions for new samples. Those require authoring passes, not architecture decisions.

---

## 6. Philosophy Flag

This document contains conclusions that transcend sample design:

- **The nine-shape taxonomy** is useful for evaluating any Precept use case, not just samples. When a customer asks "can Precept handle our workflow?", the shape taxonomy provides a structural answer.
- **The ACORD field-editability patterns** demonstrate that state-scoped `edit` declarations are not a nice-to-have — they are the mechanism real enterprises use to govern data integrity across lifecycles. This strengthens the product's positioning claim about governed integrity.
- **The insurance-depth finding** (three samples, not one) establishes a pattern: Tier 1 domains should have depth, not just presence. This applies to any domain suite planning, not just insurance.

---

## 7. Source Index

### Research sources
- ACORD Reference Architecture: https://www.acord.org/standards-architecture/reference-architecture
- ACORD State Information Guides: https://www.acord.org/forms-pages/acord-forms/forms-filing-requirements
- ACORD Data Standards (Hicron): https://hicronsoftware.com/blog/acord-data-standards-insurance/
- ACORD Forms Guide (Sonant): https://www.sonant.ai/blog/acord-forms
- ACORD Model Viewer: https://www.pilotfishtechnology.com/acord-model-viewer/
- Guidewire PolicyCenter: https://www.guidewire.com/products/core-products/insurancesuite/policycenter-insurance-policy-administration
- Guidewire PolicyCenter Transactions: https://learnguidewire.com/a-complete-guide-to-policy-transactions-in-guidewire-policycenter/
- Guidewire ClaimCenter: https://www.guidewire.com/products/core-products/insurancesuite/claimcenter-claims-management-software
- Guidewire Policy Lifecycle (YouTube): https://www.youtube.com/watch?v=S8uNdpQWw2c
- ServiceNow Change State Model: https://www.servicenow.com/docs/r/xanadu/it-service-management/change-management/c_ChangeStateModel.html
- UML State Machine Reference: https://www.uml-diagrams.org/state-machine-diagrams-reference.html
- UML State Machine Tutorial (Sparx): https://sparxsystems.com/resources/tutorials/uml2/state-diagram.html
- Approval Workflow Patterns (cFlowApps): https://www.cflowapps.com/approval-workflow-design-patterns/
- Agent Governance Approval Gates: https://github.com/Adirtr/agent-governance-schema/blob/main/articles/03-approval-gates.md
- SAP Closed-Loop Change Management: https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-sap/closed-loop-change-management-in-cloud-and-onprem-with-sap-s-4hana-change/ba-p/13515217

### Prior team research
- `frank-enterprise-platform-and-research-gaps.md` — platform survey, eight lifecycle shapes, research lanes
- `frank-entity-modeling-addendum.md` — entity-vs-workflow correction, three archetype model
- `frank-language-and-philosophy.md` — realism criteria, feature-to-sample traceability
- `frank-sample-ceiling-philosophy-addendum.md` — domain-fit tiers, dilution test
- `peterman-enterprise-ecosystem-benchmarks.md` — enterprise platform evidence
- `peterman-realistic-domain-benchmarks.md` — domain-by-domain external research

---

*This document is a research artifact for the sample-realism initiative. It delivers Research Lane 3 (state-graph shape taxonomy) from the enterprise platform survey, combined with an ACORD/insurance deep study that grounds the taxonomy in the most evidence-rich domain available.*
