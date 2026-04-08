# Enterprise Platform Survey & Research Gaps for Sample Corpus

**Author:** Frank (Lead Architect & Language Designer)
**Date:** 2026-07-18
**Status:** Research artifact — input for sample-realism initiative
**Depends on:** `frank-language-and-philosophy.md`, `frank-sample-ceiling-philosophy-addendum.md`, `peterman-realistic-domain-benchmarks.md`

---

## Purpose

Shane asked: "Is there any other research we could do that would also help to improve our sample set?"

This document answers that question in two ways:

1. **Platform survey** (§1–§3): What do enterprise workflow, case management, and business rules platforms actually build? Where does Precept's model fit — and where does it not?
2. **Research gap analysis** (§4–§5): What additional research lanes, beyond platform benchmarking, would materially improve the sample corpus? Ranked by value.

The goal is practical. Every finding either justifies a concrete new sample, improves an existing one, or identifies a research thread worth pursuing.

---

## 1. Enterprise Platform Survey

I surveyed eight platforms that collectively define the enterprise workflow and business rules market. Each was evaluated for: lifecycle shapes, policy/rule patterns, domain nouns, and Precept fit.

### 1.1 Platform Summaries

#### A. Salesforce (Flow, Industries/Vlocity, Service Cloud)

Salesforce models case management through Service Cloud, process automation via Flow Builder, and industry-specific templates through Industries (formerly Vlocity) with OmniStudio.

**Lifecycle shapes observed:**
- Insurance (Policy): New Business → Underwriting → Issuance → Endorsements → Renewal → Cancellation
- Insurance (Claim): FNOL → Documentation → Adjudication → Settlement → Closed
- Healthcare: Patient Intake → Utilization Management → Claims → Appeals
- Financial Services: Customer Onboarding → KYC → Product Application → Servicing → Complaint
- General CRM: Case Open → Assignment → Investigation → Resolution → Closed

**Policy patterns:** Amount-threshold approvals, SLA-breach auto-escalation, case-type routing rules, multi-factor eligibility determination, claim-type document checklists.

**Precept fit:** Case lifecycle (one entity, explicit states, guards) is an excellent fit. So are eligibility rules, document-completeness gates, and threshold-based approval guards. Bad fit: lead-to-opportunity entity transformation, multi-case routing, OmniStudio wizard flows (UI orchestration).

**Sources:**
- [Salesforce Claims Management Foundations (Trailhead)](https://trailhead.salesforce.com/content/learn/modules/insurance-claims-foundations/meet-claims-management)
- [Salesforce Industries Data Model](https://help.salesforce.com/s/articleView?id=ind.v_data_models_vlocity_insurance_and_financial_services_data_model_667142.htm)
- [Vlocity Insurance Workflows (Ksolves)](https://www.ksolves.com/case-studies/salesforce/salesforce-vlocity-insurance-workflows)
- [Salesforce FSC Insurance Guide (Vantage Point)](https://vantagepoint.io/blog/sf/salesforce-financial-services-cloud-for-insurance-the-complete-guide-to-policy-and-claims-management)
- [Flow Approval Process (Salesforce Ben)](https://www.salesforceben.com/salesforce-spring-25-release-new-flow-approval-process-capabilities/)

#### B. ServiceNow (ITSM, HRSD, Legal, Security Operations)

ServiceNow's core is ITSM, but it extends to HR Service Delivery, Legal Service Delivery, and Security Operations. Every module follows the same structural pattern.

**Lifecycle shapes observed:**

| Module | States | Key transitions |
|--------|--------|-----------------|
| **Incident** | New → Assigned → In Progress → On Hold → Resolved → Closed | Loopback: Resolved → In Progress (reopen). Escalation from In Progress → Escalated → Reassigned. Auto-close after timeout in Resolved. |
| **Problem** | New → Assessed → Root Cause Analysis → Fix in Progress → Resolved → Closed | Known Error documentation as intermediate artifact |
| **Change (Normal)** | New → Assess → Authorize → Scheduled → Implement → Review → Closed | CAB approval at Authorize gate. Risk-level-based branching. |
| **Change (Standard)** | New → Scheduled → Implement → Review → Closed | Pre-approved template — bypasses Assess/Authorize |
| **Change (Emergency)** | New → Authorize → Implement → Review → Closed | Expedited path |
| **Service Request** | Requested → Approved → Fulfillment → Closed | Catalog-driven, optional approval based on type/cost |
| **Security Incident** | Detected → Triaged → Investigation → Containment → Remediation → Closed | Containment as distinct phase |
| **HR Case** | Submitted → Assigned → In Progress → Resolved → Closed | Cross-department task coordination |
| **Legal Request** | Intake → Triage → Assignment → In Progress → Review → Closed | Confidentiality controls |

**Dominant shape:** Intake → Triage/Classify → Assignment → Work → Review → Close, with two recurring variants: (1) approval-gated execution (Change Management) and (2) escalation-loop (Incident/Problem).

**Policy patterns:** Impact × Urgency → Priority (decision table), priority-level SLA timers, SLA breach escalation, change risk → CAB approval, category + location → assignment group, state transition guards (cannot close without resolution notes).

**Precept fit:** Incident, Change Request, Service Request, and Security Incident lifecycles are excellent fits. Priority-as-decision-table maps to computed fields. SLA enforcement maps to guards/constraints. Bad fit: CMDB relationships, cross-ticket linking, multi-department onboarding orchestration.

**Sources:**
- [ServiceNow Incident State Model](https://www.servicenow.com/docs/r/xanadu/it-service-management/incident-management/c_IncidentManagementStateModel.html)
- [ServiceNow Change State Model](https://www.servicenow.com/docs/r/xanadu/it-service-management/change-management/c_ChangeStateModel.html)
- [ServiceNow HR Service Delivery (a3logics)](https://www.a3logics.com/blog/servicenow-hr-service-delivery/)
- [ServiceNow Legal Service Delivery (Cyntexa)](https://cyntexa.com/blog/servicenow-legal-service-delivery-lsd-everything-you-need-to-know/)
- [ServiceNow Beyond IT (Betsol)](https://www.betsol.com/blog/servicenow-beyond-it-hr-facilities-legal/)
- [ITIL Incident Lifecycle (RSI Security)](https://blog.rsisecurity.com/5-steps-of-the-incident-management-lifecycle/)

#### C. Pega (Case Management, Decisioning)

Pega's entire platform is organized around the Case Lifecycle: Case Types → Stages → Processes → Steps. Every business process is a Case that moves through stages. This is the enterprise platform closest to Precept's architectural model.

**Lifecycle shapes observed:**
- KYC/Client Onboarding: Data Collection → Identity Verification → Risk Assessment → Enhanced Due Diligence → Approval → Active Client
- Healthcare Claims (Smart Claims Engine): Intake → Validation → Editing → Adjudication → Payment/Denial → Appeal
- Prior Authorization: Request → Clinical Review → Approval/Denial → Appeal
- Insurance Claims: FNOL → Assignment → Investigation → Evaluation → Settlement → Closure
- Insurance Policy: Quote → Underwrite → Issue → Endorse → Renew → Cancel

**Policy/rule patterns:** Eligibility rules (age, income, geography → eligible/ineligible), routing/assignment (complexity + type → specialist queue), stage-level SLA timers, validation rules (required documents per case type), approval thresholds (claim value > $X → supervisor), exception triggers (fraud indicators → special investigation stage). Pega's decisioning engine (Customer Decision Hub) adds ML-driven next-best-action — beyond Precept's scope, but the eligibility/scoring/filtering subset is directly relevant.

**Precept fit:** Pega's Stage → Stage model maps directly to Precept's state model. Claims case, KYC case, prior authorization — all single-entity lifecycles with guards and constraints. Bad fit: parallel child-case orchestration, next-best-action ML, multi-case queuing.

**Sources:**
- [Pega Case Lifecycle Elements](https://docs.pega.com/bundle/platform/page/platform/case-management/case-life-cycle-elements.html)
- [Pega Academy — Case Life Cycle](https://academy.pega.com/topic/case-life-cycle/v2)
- [Pega Smart Claims Engine](https://docs.pega.com/bundle/smart-claims-engine/page/smart-claims-engine/product-overview/pega_smart_claims_engine_for_healthcare_overview-con.html)
- [Pega KYC / CLM (EY Alliance)](https://www.ey.com/en_gl/alliances/pegasystems/know-your-customer)
- [Pega CDH (pegagang)](https://www.pegagang.com/post/pega-cdh-training-certification-guide)
- [Pega Decision Strategy Components](https://community.pega.com/sites/pdn.pega.com/files/help_v85/rule-/rule-decision-/rule-decision-strategy/decision_strategy_components-ref.htm)

#### D. Appian (Case Management, Regulated Industries)

Appian promotes Case Management as a Service (CMaaS) — modular, composable case handling. Its strongest domain is public sector and regulated industries.

**Lifecycle shapes observed:**
- Investigation: Intake → Triage → Investigation → Finding → Resolution → Closure
- Regulatory compliance: Submission → Review → Compliance Check → Approval/Rejection → Notification
- Benefits/Entitlement: Application → Eligibility Determination → Approval → Disbursement → Monitoring
- Permit/License: Application → Review → Inspection → Issuance/Denial
- Criminal justice: Case Opened → Investigation → Prosecution Prep → Court → Disposition

**Dominant shape (especially public sector):** Application/Intake → Eligibility/Triage → Review/Investigation → Decision → Fulfillment/Notification → Monitoring/Appeal → Closure. The **appeal loop** (Decision → Appeal → Re-review → Decision) is a distinctive recurring pattern in regulated industries.

**Policy patterns:** Multi-factor eligibility (age, income, residency), document-type checklists, case-age SLA escalation, value/risk-based approval routing, automated audit trails.

**Precept fit:** Permit/license application, benefits application with eligibility, investigation with appeal loop, regulatory compliance case — all excellent fits. Bad fit: cross-departmental task coordination, data fabric integrations, RPA actions.

**Sources:**
- [Appian Public Sector Case Management (PR Newswire)](https://www.prnewswire.com/news-releases/announcing-appian-case-management-for-public-sector-301971457.html)
- [Appian Case Management Transformation (Yexle)](https://yexle.com/blog/public-sector-appian-case-management-transformation)
- [Appian for Public Sector (Coforge)](https://www.coforge.com/what-we-do/capabilities/appian/appian-for-public-sector)

#### E. Camunda / BPMN / DMN

DMN decision tables are the most standardized business rule representation across all platforms. BPMN provides the process flow.

**DMN decision table patterns:**
1. **Eligibility/Qualification** — inputs: age, income, credit; output: eligible yes/no, reason. Hit policy: First or Unique.
2. **Risk Assessment** — inputs: amount, history, location; output: risk level, requires review.
3. **Routing/Assignment** — inputs: type, priority, geography; output: team, SLA level.
4. **Approval Authority** — inputs: amount, type; output: approval level required.
5. **Pricing/Rating** — inputs: tier, product, quantity; output: discount, unit price.

**Single-entity BPMN lifecycle patterns:** Order (Created → Validated → Approved → Fulfilled → Shipped → Closed), Invoice (Received → Matched → Approved → Paid → Archived), Request (Submitted → Reviewed → Approved/Rejected → Executed → Closed).

**Key architectural insight: DMN tables guard BPMN transitions.** At each gateway, a DMN table evaluates whether the entity proceeds, what path it takes, or what data gets set. This maps directly to Precept: guards on events ARE the decision logic at transition points. Precept collapses BPMN process + DMN rules into a single artifact.

**Precept fit:** DMN eligibility/risk/routing tables → Precept guards. Single-entity lifecycle processes → Precept state machines. Bad fit: multi-party BPMN swimlanes, message-passing between processes, timer events, complex subprocess hierarchies.

**Sources:**
- [Camunda DMN Quick Start](https://docs.camunda.org/get-started/quick-start/decision-automation/)
- [Camunda DMN Tutorial](https://camunda.com/dmn/)
- [Camunda BPM Examples (GitHub)](https://github.com/camunda/camunda-bpm-examples)
- [BPMN.org Specification](https://www.bpmn.org/)
- [DMN Decision Notation Tutorial (ProcessMaker)](https://www.processmaker.com/blog/decision-model-and-notation-dmn-tutorial-examples/)

#### F. IBM ODM / BAW

IBM ODM is the market's most mature standalone business rules engine. IBM BAW provides case management.

**ODM rule patterns:**
1. **Action Rules** (if-then): `if customer.age < 21 then set eligibility = "ineligible"` — written in Business Action Language (BAL).
2. **Decision Tables**: Multi-factor eligibility (age range × claims history → eligible/ineligible).
3. **Rule Flows**: Ordered execution (validate income → check credit → verify compliance → determine eligibility).
4. **Parameterized Rules**: Externalized thresholds configurable without code changes.

**BAW case management:** Stage-and-activity-based (Submission → Initial Review → Approval → Completion). Sequential, parallel, and ad-hoc approval patterns. Document lifecycle integration via FileNet.

**Policy patterns:** Compound-condition eligibility, threshold-based escalation, parameterized policy limits, sequential validation, document-gated progression.

**Precept fit:** ODM's action rules and decision tables map directly to Precept guards and constraints. BAW case lifecycle maps to Precept states/events. Bad fit: ODM rule flows with complex execution ordering, FileNet document management, parallel approval with quorum.

**Sources:**
- [IBM ODM Tutorial](https://www.ibm.com/docs/en/odm/8.8.0?topic=tutorials-tutorial-getting-started-business-rules)
- [ODM Rules Components and Types (Salient Process)](https://salientprocess.com/blog/odm-rules-components-types/)
- [IBM BAW Product Overview](https://www.ibm.com/docs/en/baw/24.0.x?topic=24x-product-overview)
- [IBM BAW Case Management](https://www.ibm.com/docs/en/baw/24.0.x?topic=24x-case-management)

#### G. Guidewire

Guidewire is THE dominant insurance-industry platform. Three modules map to three core insurance entity lifecycles.

**PolicyCenter — Policy Lifecycle:**
Draft → Submitted → Quoted → Underwriting → Approved → Issued/Bound → Active → Cancelled/Expired. Key events: Submit, Quote, Approve, Issue, Endorse (mid-term change), Renew, Cancel, Reinstate. The **endorsement pattern** (modify an active policy mid-term → returns to review → reactivates) is distinctive. Guards: underwriting rules, risk assessment, product eligibility, territory restrictions.

**ClaimCenter — Claim Lifecycle:**
Open (FNOL) → Assigned → Investigation → Evaluation → Negotiation → Settlement → Closed/Denied. Key events: Report (FNOL), Assign, Investigate, Evaluate, Settle, Deny, Reopen, Close. Guards: coverage verification, reserve limits, authority levels, fraud indicators. Subrogation and salvage as sub-lifecycles.

**BillingCenter — Billing Lifecycle:**
Invoice Generated → Payment Due → Paid / Overdue → Collections → Written-off / Closed. Commission calculation, delinquency tracking with escalation.

**Rule/eligibility patterns:** Risk factors → eligibility/premium tier/required inspections, policy type + geography → available coverages, claim value + adjuster level → settlement authority, claim type + severity → initial reserve, fraud-indicator scoring, claims count + payment history → auto-renew vs non-renew.

**Precept fit:** Claim lifecycle is the #1 canonical case. Policy lifecycle with endorsement loop is excellent. Billing delinquency lifecycle is good. Bad fit: multi-policy portfolio management, cross-entity billing integration, reinsurance cascades.

**ACORD standard alignment:** Guidewire implements the ACORD Reference Architecture — the insurance industry's canonical data and process model for policy and claim lifecycle states. ACORD defines standardized lifecycle states for both policies (Quote → Application → Underwriting → Issuance → Endorsement → Cancellation → Renewal → Expiry) and claims (FNOL → Acknowledgment → Investigation → Adjustment → Settlement → Closure → Reopen). This gives Precept insurance samples an industry-standard reference model to validate against.

**Sources:**
- [Guidewire Tutorial (TechSolidity)](https://techsolidity.com/blog/guidewire-tutorial)
- [Guidewire PolicyCenter Transactions (LearnGuidewire)](https://learnguidewire.com/a-complete-guide-to-policy-transactions-in-guidewire-policycenter/)
- [Guidewire PolicyCenter (Guidewire.com)](https://www.guidewire.com/products/core-products/insurancesuite/policycenter-insurance-policy-administration)
- [ACORD Reference Architecture](https://www.acord.org/standards-architecture/reference-architecture)
- [ACORD Data Standards (Hicron)](https://hicronsoftware.com/blog/acord-data-standards-insurance/)

#### H. Temporal

Temporal models workflows as durable, long-running code. It distinguishes between single-entity lifecycle patterns and multi-service orchestration.

**Entity Lifecycle (Actor Pattern) — Good Precept fit:** Each entity instance (Account, Subscription, Order) is a dedicated long-running workflow. The workflow persists state and enforces invariants. Example: Subscription lifecycle — Trial → Active → Overdue → Suspended → Cancelled. Signals represent external events; queries represent state inspection.

**Approval Flows (Human-in-the-Loop) — Good fit:** Workflow waits for approval signal with timeout and escalation. Maps to Precept's event-with-guard pattern.

**Saga Pattern (Multi-Service Orchestration) — Bad fit:** Reserve Inventory → Charge Payment → Ship → Notify with compensation logic. This is orchestration across multiple entities/services.

**Precept fit:** Entity-as-actor pattern is a good fit. Subscription lifecycle, approval workflows — good. Bad fit: saga pattern, timer-based scheduling, activity fan-out, event choreography.

**Sources:**
- [Temporal Workflow Documentation](https://docs.temporal.io/workflows)
- [Temporal Workflow Patterns (Keith Tenzer)](https://keithtenzer.com/temporal/Temporal_Fundamentals_Workflow_Patterns/)
- [Saga Pattern (microservices.io)](https://microservices.io/patterns/data/saga.html)

### 1.2 Adjacent Standards: CMMN and DDD

Two additional frameworks provide structural insight:

**CMMN (Case Management Model and Notation)** — The OMG standard for adaptive case management. CMMN uses Case Plan Models with Stages, Tasks, Milestones, and Sentries (event-condition-action triggers). Unlike BPMN's process-centric model, CMMN is data-centric and declarative — tasks are activated based on case data, not sequence. Stages and sentries map directly to Precept's states and guards. The key CMMN insight for Precept: real case management is often more adaptive (data-driven branching) than sequential. Our complex samples should reflect this.

**DDD Aggregates** — In Domain-Driven Design, an Aggregate is a cluster of entities treated as a unit with an Aggregate Root that enforces invariants. The aggregate lifecycle — creation, mutation through business operations, invariant enforcement at each transition — maps almost exactly to a Precept definition. This is not coincidental. Precept IS a DSL for declaring aggregate behavior.

**Sources:**
- [CMMN Specification (OMG)](https://www.omg.org/cmmn/)
- [CMMN Guide (Visual Paradigm)](https://skills.visual-paradigm.com/docs/cmmn-explained-practical-guide-for-modelers/foundations-of-case-management-and-cmmn/cmmn-overview-adaptive-case-management-2026/)
- [DDD Bounded Contexts (Martin Fowler)](https://martinfowler.com/bliki/BoundedContext.html)
- [DDD Reference (Eric Evans)](https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf)

---

## 2. Cross-Platform Synthesis

### 2.1 Recurring Lifecycle Shapes

Eight lifecycle shapes recur across three or more platforms. This is the evidence base for which sample shapes the corpus should cover.

| # | Shape | Platforms | Current coverage | Priority |
|---|-------|-----------|-----------------|----------|
| 1 | **Intake → Review → Decision → Fulfillment → Close** (universal case) | All 8 | ✅ Well covered: loan, rental, badge request, reimbursement | Low — already saturated |
| 2 | **Intake → Evidence Loop → Adjudication → Settlement/Denial** (claims) | Salesforce, Pega, Guidewire, Appian, IBM | ✅ Covered: `insurance-claim.precept` | Low — could deepen but slot exists |
| 3 | **Submission → Risk Assessment → Approval → Active → Renewal/Cancel** (policy/contract) | Salesforce, Pega, Guidewire, IBM | ⚠️ Partial: `loan-application` stops at approval | **High** — missing post-approval active life |
| 4 | **New → Assess → Authorize → Execute → Review → Close** (change/procurement) | ServiceNow, Pega, Appian, IBM | ❌ Not represented | **Highest** — authorization gate is the missing shape |
| 5 | **Detected → Triage → Investigation → Containment → Remediation → Close** (security/compliance) | ServiceNow, Pega, Appian | ❌ Not represented | **High** — containment is distinctive |
| 6 | **Application → Eligibility → Approved/Denied → Appeal → Re-review → Final** (benefits/regulated) | Appian, Salesforce, Pega | ❌ Not represented | **High** — appeal loop is the missing pattern |
| 7 | **Draft → Active → Amendment → Active → Renewed/Expired** (long-lived contract) | Salesforce, Guidewire, Pega, IBM | ❌ Not represented | **High** — amendment/endorsement loop |
| 8 | **Request → Fulfillment → Confirmation → Closed** (simple service) | ServiceNow, Salesforce, Pega | ✅ Partial: badge request, parcel locker | Low — adequately covered |

**Verdict:** Shapes 4, 5, 6, and 7 are genuinely missing lifecycle patterns. Shape 3 is partially present but needs extension. These five gaps should drive the next sample wave.

### 2.2 Recurring Policy Shapes

These policy/rule patterns appear across 3+ platforms and should be visible in the sample corpus:

| Policy pattern | Platforms | Current sample coverage | Gap |
|---------------|-----------|------------------------|-----|
| **Multi-factor eligibility** (age + income + score → eligible) | All | `loan-application` (compound guard) | Need 2+ more examples |
| **Approval threshold** (amount → approval level) | SF, SN, Pega, Appian, IBM | `travel-reimbursement` (ratio check) | Need explicit threshold-routing sample |
| **Document completeness gate** (required docs → can proceed) | SF, Pega, Appian, GW, IBM | `insurance-claim` (set of documents) | Need 1+ with stricter gate logic |
| **Risk classification** (factors → Low/Med/High) | GW, Pega, IBM, Camunda | Not explicitly modeled | Need with `choice` type (FUTURE(#25)) |
| **Priority matrix** (impact × urgency → priority) | SN, Pega, IBM | `it-helpdesk-ticket` (severity as number) | Need with `choice` (FUTURE(#25)) |
| **Authority level** (value + role → authorized) | GW, SN, Pega | Not modeled | Good pattern for complex samples |
| **SLA/deadline enforcement** | SN, SF, Pega, Appian | Modeled as numeric fields | Need better `date` support (FUTURE(#26)) |
| **Escalation trigger** (condition → force state change) | SN, SF, Pega | `it-helpdesk-ticket` (escalation event) | Need 1+ more with explicit escalation |

### 2.3 Recurring Domain Nouns

**Tier 1 — Universal (5+ platforms):** Case, Claim, Request, Application, Incident, Policy, Order

**Tier 2 — Common (3–4 platforms):** Contract, Ticket, Invoice, Approval, Investigation, Matter, Enrollment, Submission

**Tier 3 — Domain-specific (1–2 platforms, but represent important lifecycle shapes):** Change Request (ITSM), Prior Authorization (Healthcare), Endorsement (Insurance), Permit (Government), Vulnerability (Security), Benefit (Social Services)

### 2.4 What Does NOT Translate to Precept

These patterns appear prominently across enterprise platforms but violate Precept's single-entity, deterministic, governed-lifecycle model:

| Anti-pattern | Examples | Why it doesn't fit |
|-------------|----------|-------------------|
| **Multi-entity orchestration** | Pega parent/child cases, Temporal sagas, ServiceNow cross-ticket linking | Precept models ONE entity |
| **Multi-actor choreography** | BPMN swimlanes, parallel quorum approval | Precept doesn't model role-based coordination |
| **Timer/schedule-driven automation** | ServiceNow SLA auto-escalation, Temporal billing cycles | Precept has no time dimension |
| **Data pipelines/ETL** | IBM ODM batch execution, Salesforce data flows | Precept operates on single instances |
| **AI/ML decisioning** | Pega Next-Best-Action, Salesforce Einstein | Precept guards are deterministic |
| **Entity transformation** | Salesforce Lead → Opportunity conversion | Precept models one entity type |
| **Complex subprocess hierarchies** | BPMN embedded subprocesses, Pega nested process flows | Precept's state model is flat |

---

## 3. Concrete Sample Recommendations from External Evidence

### 3.1 Highest-Impact New Samples (ranked by cross-platform evidence strength)

| Rank | Sample | Shape | Evidence source | Key construct pressure |
|------|--------|-------|-----------------|----------------------|
| 1 | **change-request** | Assess → Authorize → Execute → Review → Close | ServiceNow (publicly specified), Pega, Appian, IBM | `choice` for type (Normal/Standard/Emergency), guards on authorization gate, risk-based branching |
| 2 | **benefits-application** | Application → Eligibility → Decision → Appeal → Re-review | Appian public sector, Pega healthcare, Salesforce | Appeal loop (state revisit), multi-factor eligibility guard, `choice` for determination reason |
| 3 | **insurance-policy-lifecycle** | Quote → Underwrite → Issue → Active → Endorse → Renew/Cancel | Guidewire, Salesforce Industries, Pega, ACORD standard | Endorsement loop, long-lived Active state, `edit` in Active, renewal/cancel branching |
| 4 | **security-incident** | Detected → Triage → Investigation → Containment → Remediation → Closed | ServiceNow SecOps, Pega, Appian | `choice` for severity, containment as distinct phase, investigation loop, mandatory postmortem before close |
| 5 | **customer-onboarding-kyc** | Collection → Verification → Risk Assessment → EDD → Approved | Pega CLM, Salesforce FSC, IBM, SWIFT KYC standard | Risk-tiered escalation, conditional EDD path, watchlist-hit flag, `set` for required documents |
| 6 | **invoice-lifecycle** | Generated → Sent → Due → Paid/Overdue → Collections → Closed | Guidewire Billing, ServiceNow, Salesforce | Delinquency escalation, partial payment tracking, `decimal` amounts (FUTURE(#27)), threshold guards |
| 7 | **building-permit** | Application → Completeness Check → Review → Decision → Appeal → Issuance | Appian public sector, Pega | Document-completeness gates, inspection scheduling, `set` for required documents, appeal loop |
| 8 | **problem-investigation** | Reported → Confirmed → Root Cause Analysis → Workaround → Fix → Verified → Closed | ServiceNow Problem Management | Investigation-focused (not restoration), Known Error documentation, different shape than `it-helpdesk-ticket` |

### 3.2 Existing Samples to Deepen

| Current sample | External evidence | Recommended improvement |
|---------------|-------------------|------------------------|
| `insurance-claim` | Guidewire ClaimCenter fraud-indicator pattern, ACORD lifecycle | Add fraud-indicator guard, authority-level threshold on settlement, explicit reopen path |
| `loan-application` | Salesforce/Pega post-approval servicing pattern | Extend past approval into Active → Servicing → Payoff/Default lifecycle |
| `subscription-cancellation-retention` | Temporal billing dunning, Guidewire BillingCenter delinquency | Add billing/dunning cycle, overdue escalation, reinstatement path |
| `it-helpdesk-ticket` | ServiceNow Incident state model (publicly specified) | Align more closely: add On Hold state, SLA escalation event, auto-close path, reopen from Resolved |

### 3.3 Samples NOT Recommended (would dilute)

These domains appeared in the platform survey but fail the dilution test from the philosophy addendum:

| Candidate | Why not |
|-----------|---------|
| **CRM lead/opportunity pipeline** | Entity transformation pattern (Lead → Opportunity). Not a governed lifecycle — it's a sales pipeline. |
| **Data pipeline ETL workflow** | No single entity with policy enforcement. Transform chains. |
| **Multi-party contract negotiation** | Requires choreography between parties. Precept can model one party's view, but the negotiation IS the multi-party aspect. |
| **IoT sensor alerting** | Time-race, continuous-signal, non-deterministic. Violates determinism commitment. |
| **Product catalog management** | No meaningful lifecycle. CRUD with no governed transitions. Wait for data-only precepts (#22) and model as data-only. |

---

## 4. What Other Research Would Improve the Sample Set?

This is the core of Shane's question. Beyond platform benchmarking, what research lanes would materially improve the corpus? Here are my ranked recommendations.

### Research Lane 1: **Domain Expert Interview Protocols** (Highest value)

**What:** Design a structured protocol for interviewing domain experts (insurance adjusters, compliance officers, HR managers, IT change managers) about their actual workflow pain points, exception frequency, and decision logic.

**Why:** Platform surveys tell us what vendors _build_. Domain expert interviews tell us what practitioners _actually do_ — including the exception paths, workarounds, and tribal knowledge that vendors' idealized models omit. Every sample realism problem we've identified (shallow guards, missing exception loops, toy-level policy density) is a symptom of writing samples from vendor documentation instead of practitioner experience.

**What it would yield:** Real guard conditions, real rejection reasons, real exception paths. The difference between `when Amount > 10000` (generic threshold) and `when ClaimAmount > PolicyDeductible + ReserveBuffer and HasPoliceReport == false` (domain-real). It would also reveal which fields, states, and events domain experts consider essential vs decorative.

**Effort:** Medium. Requires 3–5 interviews per target domain. Could be structured as "review our current sample and tell us what's wrong."

### Research Lane 2: **Regulatory Requirement Mining** (Very high value)

**What:** Systematically mine regulatory frameworks (CMS healthcare rules, NAIC insurance model laws, SOX compliance requirements, FINRA AML programs, OSHA incident reporting, ITIL/ISO 20000 for ITSM) for specific, citable business rules that Precept samples should encode.

**Why:** Regulatory requirements are the gold standard for "real business rules." They are published, specific, and non-negotiable. A sample guard that encodes an actual CMS prior-authorization turnaround requirement (72 hours urgent, 7 days standard — already cited in Peterman's benchmarks) is more credible than any invented threshold. Mining regulatory sources would give our samples citable, verifiable rules instead of plausible-sounding approximations.

**What it would yield:** Specific numeric thresholds, required document lists, mandatory review windows, escalation triggers, and denial-reason taxonomies that can be encoded directly as guards, constraints, and `choice` values. Would dramatically improve the prior-authorization, KYC, change-management, and insurance samples.

**Effort:** Medium-high. Requires reading regulatory source material. CMS and NAIC publish extensively. ITIL frameworks are well-documented.

### Research Lane 3: **State-Graph Shape Taxonomy** (High value)

**What:** Formalize a taxonomy of lifecycle graph shapes — linear, branching, looping (evidence loop, appeal loop, amendment loop), diamond (converging paths), and complex (multiple loops + branches). Map every current and proposed sample to its shape. Identify which shapes are over- and under-represented.

**Why:** The platform survey revealed that specific lifecycle shapes (appeal loop, endorsement loop, containment phase, authorization gate) are structural patterns, not just domain-specific details. A shape taxonomy would give the team a language for discussing what's missing that's more precise than "we need more complex samples." It would also prevent the failure mode of adding samples that look different (new domain name) but have identical graph structure (intake → review → approve → close).

**What it would yield:** A 2D map: shape-type × domain-family. Empty cells = genuine gaps. Filled cells = coverage. This replaces the current approach of evaluating samples one at a time with a portfolio view.

**Effort:** Low-medium. Can be done mechanically from current samples + proposed candidates.

### Research Lane 4: **Guard/Constraint Density Benchmarking** (High value)

**What:** Benchmark the guard and constraint density of our samples against real rule sets from published sources — DMN decision tables (Camunda examples, IBM ODM tutorials), Guidewire underwriting rule configurations, ServiceNow business rules, and Pega decisioning strategies.

**Why:** Our existing research (frank-language-and-philosophy.md §2.4) identified the "shallow guard" problem. But we haven't quantified how dense real-world rule sets actually are. How many conditions does a real underwriting eligibility rule have? How many inputs does a typical DMN routing table evaluate? If real-world guards routinely have 5–8 conditions and our samples average 1–2, the gap is measurable and the fix is specific.

**What it would yield:** Target density metrics. "A credible complex sample should have at least N compound-condition guards and M constraints." This gives sample authors a measurable bar, not just "make it more realistic."

**Effort:** Medium. Requires analyzing published rule examples from vendor tutorials and documentation.

### Research Lane 5: **AI Authoring Friction Study** (High value)

**What:** Test whether an AI agent (using `precept_compile`, `precept_inspect`, and `precept_fire`) can author, validate, and iterate on each current sample from a plain-English domain description. Measure: how many MCP tool calls to reach a correct definition, which constructs cause the most iteration, which domain descriptions produce the most divergent results.

**Why:** Precept is AI-first (Principle 12). Our samples are training data for AI authoring. But we haven't tested whether AI agents can actually reproduce the samples from domain descriptions. If an AI consistently gets the same domain "wrong" — wrong states, wrong guards, wrong event structure — that's signal about either the domain description, the sample design, or the language surface. The friction points reveal what to fix.

**What it would yield:** A ranked list of authoring friction points. Which constructs are hardest for AI? Which domains require the most iteration? Which samples are easiest/hardest to reproduce? This directly informs both sample design and language evolution.

**Effort:** Medium. Can be partially automated using MCP tools.

### Research Lane 6: **Exception-Path Frequency Analysis** (Moderate value)

**What:** For each major domain in the corpus (insurance, finance, compliance, ITSM, healthcare), research how frequently exception paths actually fire in real operations. What percentage of claims get denied? Reopened? Escalated? What percentage of change requests get rejected at CAB? What percentage of KYC cases hit Enhanced Due Diligence?

**Why:** Samples need to model exception paths. But if a sample gives equal weight to a path that fires 0.1% of the time and one that fires 40% of the time, it misrepresents the domain. Real operations data — even rough industry averages — would help sample authors weight their state graphs and guard conditions appropriately.

**What it would yield:** Realistic probability distributions for exception paths. "In insurance, ~15-20% of claims require additional investigation, ~5-10% are denied, ~2% are reopened." This helps sample authors decide which exception paths are essential (always model) vs rare (model if space permits).

**Effort:** Medium. Industry reports, regulatory disclosures, and vendor case studies often publish these figures.

### Research Lane 7: **Competitive Sample Corpus Audit** (Moderate value)

**What:** Audit the official sample/example corpora of XState, Temporal, AWS Step Functions, and Drools/KIE for: domain distribution, complexity gradient, rule density, and which domains they avoid.

**Why:** Peterman's benchmarks (peterman-sample-corpus-benchmarks.md) already surveyed corpus SIZE and SHAPE across these projects. But we haven't compared DOMAIN COVERAGE or RULE DENSITY. Which domains do XState's 49 examples cover? Which domains does Temporal's 75 sample directories cover? If every workflow tool avoids the same domains (healthcare? insurance? government?), that's signal about domain difficulty. If they all cluster in the same domains, that's signal about what the market expects.

**What it would yield:** A competitive positioning map. Where does Precept's corpus lead, match, or trail the adjacent ecosystem? Which domains are white space that only Precept covers?

**Effort:** Low-medium. Corpus listings are publicly available.

### Research Lane 8: **Collection-Pattern Catalog** (Lower value but targeted)

**What:** Research how real-world entities use collections (lists, sets, queues) in their domain data. What does a real insurance claim's document collection look like? How does a real helpdesk ticket's activity log accumulate? What ordering matters (queue vs set vs stack)?

**Why:** 14 of 21 current samples use collections, but most use them superficially. `set of string` with `add` and `.count` is the dominant pattern. Real domain collections have more structure: ordered activity logs (queue), unique-document-type sets (set), undo stacks (stack). Understanding real collection usage would make our samples' collection patterns more credible.

**What it would yield:** Specific collection-usage patterns for 6–8 domains. "An insurance claim typically accumulates 5–15 documents of 3–5 types, with type uniqueness enforced and completeness checked." This informs both sample design and collection-related language proposals.

**Effort:** Low-medium. Can be researched from industry documentation.

### Research Lane Summary (Ranked)

| Rank | Lane | Value | Effort | Why this rank |
|------|------|-------|--------|---------------|
| 1 | Domain expert interview protocols | Highest | Medium | Addresses root cause of shallow samples: we write from docs, not experience |
| 2 | Regulatory requirement mining | Very high | Medium-high | Provides citable, verifiable, non-negotiable rules for guards and constraints |
| 3 | State-graph shape taxonomy | High | Low-medium | Prevents structural duplication; enables portfolio gap analysis |
| 4 | Guard/constraint density benchmarking | High | Medium | Gives measurable targets for sample quality; quantifies the shallow-guard gap |
| 5 | AI authoring friction study | High | Medium | Validates the AI-first promise on real samples; reveals language surface issues |
| 6 | Exception-path frequency analysis | Moderate | Medium | Helps weight exception paths realistically; prevents over/under-representation |
| 7 | Competitive sample corpus audit | Moderate | Low-medium | Competitive positioning; identifies white space |
| 8 | Collection-pattern catalog | Lower | Low-medium | Targeted improvement for a specific construct family |

---

## 5. Decisions

### 5.1 What I'm deciding

1. **The four missing lifecycle shapes (§2.1, shapes 4–7) are genuine gaps that the next sample wave must fill.** The authorization-gate, appeal-loop, containment-phase, and amendment-loop patterns are not wishlist items — they are the most common enterprise lifecycle shapes that our corpus does not represent. This is an evidence-based finding from eight independent platform surveys.

2. **The top 3 research lanes (domain expert interviews, regulatory mining, shape taxonomy) should be prioritized before the next major sample authoring pass.** Writing more samples without this research risks producing more plausible-looking-but-domain-shallow files. The research is a quality investment, not a delay.

3. **The platform survey itself should be a living reference, not a one-shot document.** As the team encounters new domain patterns in customer conversations, issue discussions, or competitive analysis, they should be added to the cross-platform synthesis tables. The eight lifecycle shapes and ten policy patterns are a starting vocabulary, not a finished catalog.

4. **CMMN and DDD aggregate alignment are worth citing in design discussions but do not need their own research lanes.** The fit is structural — Precept IS an aggregate lifecycle language — and the validation is already implicit in the platform survey results. No separate research artifact needed.

### 5.2 What I'm NOT deciding

- Whether any specific sample from §3.1 actually gets written. That's a portfolio decision for the team.
- Which research lanes are funded. That's Shane's call.
- The priority ordering of the eight new sample candidates. That depends on which language features ship when.

---

## 6. Source Index

### Platform documentation
- ServiceNow Incident State Model: https://www.servicenow.com/docs/r/xanadu/it-service-management/incident-management/c_IncidentManagementStateModel.html
- ServiceNow Change State Model: https://www.servicenow.com/docs/r/xanadu/it-service-management/change-management/c_ChangeStateModel.html
- Pega Case Lifecycle Elements: https://docs.pega.com/bundle/platform/page/platform/case-management/case-life-cycle-elements.html
- Pega Academy Case Life Cycle: https://academy.pega.com/topic/case-life-cycle/v2
- Pega Smart Claims Engine: https://docs.pega.com/bundle/smart-claims-engine/page/smart-claims-engine/product-overview/pega_smart_claims_engine_for_healthcare_overview-con.html
- Camunda DMN Quick Start: https://docs.camunda.org/get-started/quick-start/decision-automation/
- Camunda DMN Overview: https://camunda.com/dmn/
- IBM ODM Tutorial: https://www.ibm.com/docs/en/odm/8.8.0?topic=tutorials-tutorial-getting-started-business-rules
- IBM BAW Product Overview: https://www.ibm.com/docs/en/baw/24.0.x?topic=24x-product-overview
- Guidewire PolicyCenter: https://www.guidewire.com/products/core-products/insurancesuite/policycenter-insurance-policy-administration
- Guidewire PolicyCenter Transactions: https://learnguidewire.com/a-complete-guide-to-policy-transactions-in-guidewire-policycenter/
- Temporal Workflow Documentation: https://docs.temporal.io/workflows
- ACORD Reference Architecture: https://www.acord.org/standards-architecture/reference-architecture

### Industry and analysis sources
- Salesforce Claims Foundations (Trailhead): https://trailhead.salesforce.com/content/learn/modules/insurance-claims-foundations/meet-claims-management
- Salesforce FSC Insurance Guide: https://vantagepoint.io/blog/sf/salesforce-financial-services-cloud-for-insurance-the-complete-guide-to-policy-and-claims-management
- Appian Public Sector Case Management: https://www.prnewswire.com/news-releases/announcing-appian-case-management-for-public-sector-301971457.html
- Appian Case Transformation: https://yexle.com/blog/public-sector-appian-case-management-transformation
- Pega KYC/CLM (EY Alliance): https://www.ey.com/en_gl/alliances/pegasystems/know-your-customer
- Pega CDH Guide: https://www.pegagang.com/post/pega-cdh-training-certification-guide
- ServiceNow HR Service Delivery: https://www.a3logics.com/blog/servicenow-hr-service-delivery/
- ServiceNow Beyond IT: https://www.betsol.com/blog/servicenow-beyond-it-hr-facilities-legal/
- Temporal Workflow Patterns: https://keithtenzer.com/temporal/Temporal_Fundamentals_Workflow_Patterns/
- Guidewire ClaimCenter Processing: https://files.sdiarticle5.com/wp-content/uploads/2025/02/Revised-ms_AJRCOS_131417_v1.pdf
- ACORD Data Standards: https://hicronsoftware.com/blog/acord-data-standards-insurance/
- DMN Tutorial (ProcessMaker): https://www.processmaker.com/blog/decision-model-and-notation-dmn-tutorial-examples/
- SLA-Aware Escalation Workflows: https://unito.io/blog/sla-aware-ticket-escalation-workflows/
- Case Management (LEADing Practice): https://www.leadingpractice.com/enterprise-standards/enterprise-modelling/case-management/
- Outcome-Based Case Closure: https://www.struto.io/blog/the-practical-guide-to-outcome-based-escalation-and-case-closure
- ITIL Ticket Life Cycle Guide: https://www.saatpro.com/2025/07/08/itil-ticket-life-cycle-guide/
- Case Management Examples (Kissflow): https://kissflow.com/workflow/case/case-management-examples/

### Standards and regulatory references
- CMMN Specification (OMG): https://www.omg.org/cmmn/
- CMMN Guide (Visual Paradigm): https://skills.visual-paradigm.com/docs/cmmn-explained-practical-guide-for-modelers/foundations-of-case-management-and-cmmn/cmmn-overview-adaptive-case-management-2026/
- BPMN.org Specification: https://www.bpmn.org/
- DDD Reference (Eric Evans): https://www.domainlanguage.com/wp-content/uploads/2016/05/DDD_Reference_2015-03.pdf
- DDD Bounded Contexts (Martin Fowler): https://martinfowler.com/bliki/BoundedContext.html
- EU Taxonomy for Sustainable Activities: https://finance.ec.europa.eu/sustainable-finance/tools-and-standards/eu-taxonomy-sustainable-activities_en
- FINRA AML Compliance: https://www.finra.org/rules-guidance/key-topics/aml
- FDIC Consumer Compliance Examination Manual: https://www.fdic.gov/consumer-compliance-examination-manual
- Basel III Framework: https://www.bis.org/bcbs/basel3.htm
- CMS Prior Authorization Final Rule: https://www.cms.gov/priorities/burden-reduction/overview/interoperability/policies-regulations/cms-interoperability-prior-authorization-final-rule-cms-0057-f

### Prior team research
- `frank-language-and-philosophy.md` — realism criteria, feature-to-sample traceability, comment protocol
- `frank-sample-ceiling-philosophy-addendum.md` — domain-fit tiers, dilution test, philosophy-driven ceiling
- `frank-sample-ceiling-analysis.md` — 40-50 optimal band, marginal-value gate
- `peterman-realistic-domain-benchmarks.md` — domain-by-domain external research, credibility signals
- `peterman-sample-corpus-benchmarks.md` — competitive corpus size/shape analysis
- `steinbrenner-sample-portfolio-plan.md` — 42-sample portfolio target, phased plan

---

*This document is a research artifact for the sample-realism initiative. It provides enterprise platform evidence, gap analysis, and ranked research lanes for improving the sample corpus. Decisions about implementation are owned by the team; decisions about language evolution are owned by Frank with Shane sign-off.*
