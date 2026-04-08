# Enterprise Platform Pattern Survey

> **Purpose**: Identify recurring workflow, case management, and business rule patterns across eight enterprise platforms to inform Precept's sample corpus. Precept models ONE entity with a governed lifecycle — explicit states, events, guards, constraints, and full inspectability. This survey maps what the enterprise world actually builds so we can ensure our samples reflect real patterns.
>
> **Date**: 2025-07-18

---

## Table of Contents

1. [Platform A: Salesforce](#a-salesforce)
2. [Platform B: ServiceNow](#b-servicenow)
3. [Platform C: Pega](#c-pega)
4. [Platform D: Appian](#d-appian)
5. [Platform E: Camunda / BPMN / DMN](#e-camunda--bpmn--dmn)
6. [Platform F: IBM ODM / BAW](#f-ibm-odm--baw)
7. [Platform G: Guidewire](#g-guidewire)
8. [Platform H: Temporal](#h-temporal)
9. [Cross-Platform Synthesis](#cross-platform-synthesis)
10. [Gap Analysis: What Precept Samples Are Missing](#gap-analysis)
11. [Anti-Patterns: What Does NOT Translate to Precept](#anti-patterns)

---

## A. Salesforce

### Workflow/Case/Rule Patterns

Salesforce models case management through its core Service Cloud (Case object lifecycle), Flow Builder (declarative process automation), and Salesforce Industries (formerly Vlocity) with OmniStudio for industry-specific templates.

**Canonical patterns:**
- **Case management**: Open → Escalated → Working → Closed. Support cases with priority, SLA timers, owner assignment, and resolution tracking.
- **Approval processes**: Submit → Approve/Reject → Final Approval → Post-approval actions. Multi-step, threshold-based approvals.
- **Lead/Opportunity lifecycle**: New → Qualified → Proposal → Negotiation → Closed Won/Lost. Sales pipeline progression with stage gates.
- **Service requests**: Intake → Routing → Fulfillment → Confirmation → Closed.

**Sources:**
- [Salesforce Industries Data Model (Insurance/Financial Services)](https://help.salesforce.com/s/articleView?id=ind.v_data_models_vlocity_insurance_and_financial_services_data_model_667142.htm)
- [Salesforce Industries Process Library](https://ecmsalesforce.com/exploring-the-new-salesforce-industries-process-library/)
- [Vlocity Insurance Workflows (Ksolves)](https://www.ksolves.com/case-studies/salesforce/salesforce-vlocity-insurance-workflows)
- [Vlocity in Salesforce (OakTree)](https://www.oaktreecloud.com/understanding-vlocity-in-salesforce/)

### Lifecycle Shapes

| Industry | Lifecycle Shape |
|----------|----------------|
| Insurance (Policy) | New Business → Underwriting → Policy Issuance → Endorsements → Renewal → Cancellation |
| Insurance (Claim) | FNOL → Documentation → Adjudication → Settlement → Closed |
| Healthcare | Patient Intake → Utilization Management → Claims → Appeals |
| Financial Services | Customer Onboarding → KYC Verification → Product Application → Servicing → Complaint |
| General CRM | Case Open → Assignment → Investigation → Resolution → Closed |

**Key shape**: Intake → Review/Verification → Decision → Fulfillment → Close. Evidence loops (return to documentation gathering) are a recurring pattern in claims and onboarding.

### Domain Nouns
Case, Lead, Opportunity, Account, Contact, Policy, Claim, Coverage, Quote, Application, Service Request, Knowledge Article.

### Policy Shapes
- **Approval thresholds**: Amount > $X requires manager approval; Amount > $Y requires VP.
- **SLA enforcement**: Case unresolved after N hours → auto-escalate.
- **Routing rules**: Case type + priority → assignment queue.
- **Eligibility determination**: Customer age, income, credit score → product eligibility.
- **Document requirements**: Claim type → required document checklist.

### Precept Fit Assessment
- ✅ **Good fit**: Case lifecycle (one case, explicit states, guards on transitions). Policy lifecycle. Approval workflow. Complaint/grievance handling.
- ✅ **Good fit**: Eligibility rules as guards. SLA enforcement as constraints on state durations.
- ❌ **Bad fit**: Lead-to-Opportunity conversion (entity transformation, not single entity). Multi-case routing across queues (orchestration). OmniStudio guided flows (multi-screen wizards, UI concern).

---

## B. ServiceNow

### Workflow/Case/Rule Patterns

ServiceNow's core is ITSM (IT Service Management), but extends to HR Service Delivery (HRSD), Legal Service Delivery (LSD), Security Operations, and Customer Service Management.

**Canonical ITSM patterns:**

**1. Incident Management**
- States: New → Assigned → In Progress → On Hold → Resolved → Closed
- Priority = f(Impact, Urgency) — a classic decision table
- SLA timers per priority level
- Escalation on SLA breach
- Knowledge article linkage on resolution

**2. Problem Management**
- States: New → Assessed → Root Cause Analysis → Fix in Progress → Resolved → Closed
- Links to child incidents
- Known Error documentation

**3. Change Management** (extremely well-documented state model)
- **Normal change**: New → Assess → Authorize → Scheduled → Implement → Review → Closed
- **Standard change**: New → Scheduled → Implement → Review → Closed (pre-approved, bypasses Assess/Authorize)
- **Emergency change**: New → Authorize → Implement → Review → Closed (expedited)
- Each state has explicit transition rules and approval gates
- CAB (Change Advisory Board) approval at the Authorize gate

**4. Service Request**
- States: Requested → Approved → Fulfillment → Closed
- Catalog-driven intake
- Approval may be optional based on request type/cost

**Sources:**
- [ServiceNow Change State Model](https://www.servicenow.com/docs/r/xanadu/it-service-management/change-management/c_ChangeStateModel.html)
- [ServiceNow Incident Workflow (Community)](https://www.servicenow.com/community/itsm-articles/servicenow-incident-workflow-how-incident-management-really-runs/ta-p/3469448)
- [ServiceNow ITSM Guide (lmteq)](https://www.lmteq.com/blogs/servicenow/servicenow-itsm-detailed-guide/)
- [ServiceNow ITSM Configuration Guide (reco.ai)](https://www.reco.ai/hub/servicenow-itsm-configuration-guide)
- [Change Management Process Flow (NGenious)](https://ngenioussolutions.com/blog/wp-content/uploads/2024/11/ServiceNow-Change-Management-Diagrams.pdf)

### Beyond ITSM

**HR Service Delivery (HRSD)**
- Employee Case lifecycle: Submitted → Assigned → In Progress → Resolved → Closed
- Onboarding lifecycle: Initiated → IT Provisioning → Facilities → Security → Complete
- Offboarding: cross-departmental task coordination

**Legal Service Delivery**
- Legal Request: Intake → Triage → Assignment → In Progress → Review → Closed
- Matter management with confidentiality controls

**Security Operations**
- Security Incident: Detected → Triaged → Investigation → Containment → Remediation → Closed
- Vulnerability: Identified → Assessed → Remediation Assigned → Fixed → Verified → Closed

**Sources:**
- [ServiceNow HR Service Delivery (a3logics)](https://www.a3logics.com/blog/servicenow-hr-service-delivery/)
- [ServiceNow Beyond IT (Betsol)](https://www.betsol.com/blog/servicenow-beyond-it-hr-facilities-legal/)
- [ServiceNow Legal Service Delivery (Cyntexa)](https://cyntexa.com/blog/servicenow-legal-service-delivery-lsd-everything-you-need-to-know/)

### Lifecycle Shapes
The dominant shape across ALL ServiceNow modules is:

**Intake → Triage/Classify → Assignment → Work → Review → Close**

With two recurring variants:
1. **Approval-gated**: Intake → Assessment → Approval → Execution → Review → Close (Change Management)
2. **Escalation-looped**: In Progress → Escalated → Reassigned → In Progress (Incident/Problem)

### Domain Nouns
Incident, Problem, Change Request, Service Request, Task, Knowledge Article, Configuration Item (CI), SLA, Assignment Group, Approval, Case (HR/Legal), Vulnerability, Security Incident.

### Policy Shapes
- **Priority matrix**: Impact × Urgency → Priority (decision table)
- **SLA timers**: Priority-level → response time, resolution time
- **Escalation rules**: SLA breach → escalate to next tier
- **Approval rules**: Change risk level → requires CAB approval
- **Assignment rules**: Category + Location → Assignment Group
- **State transition guards**: Cannot close without resolution notes; cannot authorize without risk assessment

### Precept Fit Assessment
- ✅ **Excellent fit**: Incident lifecycle. Change Request lifecycle (Normal/Standard/Emergency as variants — or one precept with guards that handle the branching). Service Request lifecycle. Security Incident.
- ✅ **Excellent fit**: Priority-as-decision-table maps to computed fields. SLA enforcement maps to guards/constraints. Approval gates map to event guards.
- ❌ **Bad fit**: CMDB relationships (multi-entity). Cross-ticket linking (Problem → Incident). Onboarding workflows that span multiple departments (orchestration).

---

## C. Pega

### Case Lifecycle Model

Pega's entire platform is organized around the **Case Lifecycle** — every business process is a Case that moves through Stages → Processes → Steps.

**Case structure:**
- **Stages**: Major milestones (e.g., Submission, Review, Approval, Resolution, Closure)
- **Processes**: Sequences of actions within each stage
- **Steps**: Individual tasks or automated actions within processes
- **Alternate stages**: Exception/non-happy-path handling

This is the closest enterprise model to Precept's state-machine paradigm.

**Sources:**
- [Pega Case Types Documentation](https://docs.pega.com/bundle/platform/page/platform/case-management/building-case-types.html)
- [PegaStack Case Types Tutorial](https://pegastack.com/tutorials/beginner/case-types-stages)
- [Pega Case Management Review (pegacourse.com)](https://pegacourse.com/discernment-pega-case-management-a-detailed-review)
- [Pega Workflows (pegagang)](https://www.pegagang.com/post/pega-case-management-and-workflows)

### Canonical Case Types and Industry Frameworks

**Financial Services:**
- KYC/Client Onboarding: Data Collection → Identity Verification → Risk Assessment → Enhanced Due Diligence → Approval → Active Client
- Account Opening, Loan Origination, Compliance Investigation
- Pega CLM (Client Lifecycle Management) models parallel work streams that converge

**Healthcare:**
- Claims Adjudication (Smart Claims Engine): Intake → Validation → Editing → Adjudication → Payment/Denial → Appeal
- Prior Authorization / Utilization Management: Request → Clinical Review → Approval/Denial → Appeal
- Member Enrollment, Provider Credentialing

**Insurance:**
- Claims Processing: FNOL → Assignment → Investigation → Evaluation → Settlement → Closure
- Policy Administration: Quote → Underwrite → Issue → Endorse → Renew → Cancel
- Underwriting Case

**Telecommunications:**
- Customer Onboarding, Service Activation, Trouble Ticket

**Sources:**
- [Pega Smart Claims Engine](https://docs.pega.com/bundle/smart-claims-engine/page/smart-claims-engine/product-overview/pega_smart_claims_engine_for_healthcare_overview-con.html)
- [Pega KYC / CLM (EY Alliance)](https://www.ey.com/en_gl/alliances/pegasystems/know-your-customer)
- [Pega CLM (The Digital Banker)](https://thedigitalbanker.com/pegasystems-and-the-new-era-in-client-lifecycle-management/)
- [Pega Customer Decision Hub](https://onestoppega.com/solution-frameworks/pega-customer-decision-hub-basics/)

### Policy/Rule Patterns
- **Eligibility rules**: Age, income, geography → product eligibility
- **Routing/assignment**: Case complexity + type → specialist queue
- **SLA management**: Stage-level SLA timers with escalation
- **Next-Best-Action (NBA)**: AI-driven decisioning for what to offer/do next — beyond Precept's scope but the eligibility subset is relevant
- **Validation rules**: Required documents per case type, data completeness checks
- **Approval thresholds**: Claim value > $X → supervisor review
- **Exception triggers**: Fraud indicators → special investigation stage

### Precept Fit Assessment
- ✅ **Excellent fit**: Pega's Stage → Stage model maps directly to Precept's state model. Claims case, KYC onboarding case, prior authorization case — all single-entity lifecycles with guards and constraints.
- ✅ **Good fit**: Pega's decision rules (eligibility, validation, routing) map to Precept guards and constraints.
- ❌ **Bad fit**: Pega's parallel child case orchestration (parent case spawns child cases). Next-Best-Action decisioning (ML/AI). Multi-case queuing and work distribution.

---

## D. Appian

### Case Management and Process Automation Patterns

Appian promotes **Case Management as a Service (CMaaS)** — modular, composable case handling that can be quickly adapted to different domains.

**Common patterns:**
- Investigation case: Intake → Triage → Investigation → Finding → Resolution → Closure
- Regulatory compliance case: Submission → Review → Compliance Check → Approval/Rejection → Notification
- Benefits/entitlement case: Application → Eligibility Determination → Approval → Disbursement → Monitoring
- License/permit case: Application → Review → Inspection → Issuance/Denial

**Sources:**
- [Appian Case Management for Public Sector (PR Newswire)](https://www.prnewswire.co.uk/news-releases/announcing-appian-case-management-for-public-sector-301971527.html)
- [Appian Public Sector Case Management (Yexle)](https://yexle.com/blog/public-sector-appian-case-management-transformation)
- [Appian for Enterprise Automation (LowCodeMinds)](https://www.lowcodeminds.com/blogs/the-complete-guide-to-appian-for-enterprise-automation-use-cases-roi-and-real-world-adoption/)

### Public-Sector and Regulated-Industry Case Studies

| Domain | Case Study | Pattern |
|--------|-----------|---------|
| Pharmaceutical regulation (EU) | European health agency automated device registration, compliance workflows, adverse-incident case management | Application → Compliance Review → Approval → Monitoring |
| Criminal justice (Australia) | Victorian OPP streamlined criminal case management, victim/witness engagement | Case Opened → Investigation → Prosecution Prep → Court → Disposition |
| Healthcare insurance (US) | Moved from fragmented ticketing to integrated case management with HIPAA compliance | Intake → Assignment → Processing → Audit → Resolution |
| Social services | Benefits administration, appeals processing, planning & permitting | Application → Eligibility → Determination → Appeal → Disbursement |

**Sources:**
- [European Health Agency (CaseStudies.com)](https://www.casestudies.com/company/appian/case-study/how-appian-helped-a-large-health-agency-transform-the-pharmaceutical-regulatory-process)
- [OPP Victoria (ITNews)](https://www.itnews.com.au/feature/reduced-case-resolution-times-and-increased-victim-and-witness-engagement-with-appian-case-management-597675)
- [Healthcare Operations (Persistent)](https://www.persistent.com/client-success/beyond-ticketing-persistent-sets-up-foundation-for-operational-scale-with-appian/)

### Lifecycle Shapes
Appian's dominant lifecycle shape (especially in public sector) is:

**Application/Intake → Eligibility/Triage → Review/Investigation → Decision → Fulfillment/Notification → Monitoring/Appeal → Closure**

The **appeal loop** (Decision → Appeal → Re-review → Decision) is a distinctive recurring pattern in regulated industries.

### Domain Nouns
Case, Application, Request, Investigation, Permit, License, Benefit, Claim, Appeal, Inspection, Matter, Compliance Record.

### Policy Shapes
- **Eligibility determination**: Multi-factor rules (age, income, residency, etc.)
- **Document requirements**: Case type → required document checklist
- **SLA enforcement**: Case age → escalation
- **Approval routing**: Case value/risk → approval chain
- **Audit requirements**: Automated audit trail per action

### Precept Fit Assessment
- ✅ **Excellent fit**: Permit/License application lifecycle. Benefits application with eligibility determination. Investigation case with decision and appeal loop. Regulatory compliance case.
- ✅ **Good fit**: Document-completeness guards. Eligibility as a compound guard. Appeal-as-revisit-to-prior-state.
- ❌ **Bad fit**: Cross-departmental task coordination (orchestration). Data fabric integrations. RPA actions.

---

## E. Camunda / BPMN / DMN

### Decision Table (DMN) Patterns

DMN decision tables are the most standardized business rule representation across all platforms. Common patterns:

**1. Eligibility/Qualification Tables**
- Inputs: age, income, credit score, employment status
- Output: eligible (yes/no), reason code
- Hit policy: First (first matching rule wins) or Unique (exactly one rule matches)

**2. Risk Assessment Tables**
- Inputs: claim amount, customer history, location
- Output: risk level (Low/Medium/High), requires manual review (yes/no)

**3. Routing/Assignment Tables**
- Inputs: case type, priority, geography
- Output: assigned team, SLA level

**4. Approval Authority Tables**
- Inputs: request amount, request type
- Output: approval level required (self, manager, director, VP)

**5. Pricing/Rating Tables**
- Inputs: customer tier, product, quantity
- Output: discount percentage, unit price

**Sources:**
- [Camunda DMN Quick Start](https://docs.camunda.org/get-started/quick-start/decision-automation/)
- [DMN Decision Chaining (GitHub)](https://github.com/camunda-consulting/camunda-7-code-examples/blob/main/snippets/dmn-decision-chaining/README.md)
- [Camunda Workflow Patterns](https://docs.camunda.io/docs/components/concepts/workflow-patterns/)

### Single-Entity Lifecycle Patterns in BPMN

BPMN literature commonly models these single-entity lifecycles:
- **Order**: Created → Validated → Approved → Fulfilled → Shipped → Closed
- **Invoice**: Received → Matched → Approved → Paid → Archived
- **Request**: Submitted → Reviewed → Approved/Rejected → Executed → Closed
- **Application**: Received → Screened → Evaluated → Decided → Notified

Each transition is typically governed by a **Business Rule Task** that invokes a DMN decision table.

### Relationship Between Decision Tables and Lifecycle Governance

This is the key architectural insight: **DMN tables guard BPMN transitions**.

- At each gateway/decision point in a BPMN process, a DMN table evaluates whether the entity should proceed, what path it takes, or what data gets set.
- Example: Order arrives → DMN: "Is order data complete?" → if No, loop back to "Request More Info"; if Yes, proceed → DMN: "Is order high risk?" → if Yes, route to manual review.
- Decision tables are versioned independently of the process, enabling rule changes without process redeployment.

**This maps directly to Precept**: guards on events ARE the decision logic at transition points. Precept collapses the BPMN process + DMN rules into a single artifact.

### Precept Fit Assessment
- ✅ **Excellent fit**: DMN eligibility/risk/routing tables → Precept guards and computed expressions. Single-entity lifecycle processes → Precept state machines.
- ✅ **Excellent fit**: The "separation of concerns" pattern (rules separate from flow) is what Precept unifies — the precept IS both the flow and the rules.
- ❌ **Bad fit**: Multi-party BPMN processes with swimlanes. Message-passing between processes. Timer-based event triggers (Precept has no time dimension). Complex subprocess hierarchies.

---

## F. IBM ODM / BAW

### IBM ODM (Operational Decision Manager) — Business Rule Patterns

ODM is the market's most mature standalone business rules engine. Its canonical patterns are:

**1. Action Rules (If-Then)**
```
if customer.age < 21 then set eligibility = "ineligible"
if policyholder.accidents > 3 then deny discount
```
Written in Business Action Language (BAL) — natural-language-style rules.

**2. Decision Tables**
Multi-factor eligibility and policy tables (identical to DMN pattern):
| Age Range | Claims in 5 Years | Eligible? |
|-----------|-------------------|-----------|
| <25 | >0 | No |
| 25–65 | 0 | Yes |
| >65 | Any | No |

**3. Rule Flows**
Sequential/hierarchical execution ordering:
1. Validate applicant income
2. Check credit score
3. Verify compliance
4. Determine eligibility outcome

**4. Parameterized Rules**
Rules with externalized thresholds: `if loan_amount > MAX_ALLOWED then flag_for_review` where MAX_ALLOWED is configurable without code changes.

**Sources:**
- [IBM ODM Tutorial](https://www.ibm.com/docs/en/odm/8.8.0?topic=tutorials-tutorial-getting-started-business-rules)
- [ODM Rules, Components and Types (SalientProcess)](https://salientprocess.com/blog/odm-rules-components-types/)
- [ODM Wikipedia](https://en.wikipedia.org/wiki/IBM_Operational_Decision_Management)
- [ODM Rules Cookbook (GitHub)](https://dmcommunity.org/2020/02/23/the-odm-rules-cookbook-by-peter-warde/)
- [Rule Execution Modes in ODM](https://balasubramanyamlanka.com/rule-engine-execution-mode-choosing-setting-odm/)

### IBM BAW (Business Automation Workflow) — Case Management

BAW's case management is stage-and-activity-based (similar to Pega):

**Case structure:**
- Case Type: blueprint with properties, stages, activities, document types, roles
- Stages: Submission → Initial Review → Approval → Completion
- Activities: tasks triggered by events (e.g., new document upload)

**Approval patterns:**
- Sequential approval: chain of approvers
- Parallel approval: multiple reviewers simultaneously, quorum-based
- Ad hoc approval: case worker assigns dynamically

**Integration with FileNet** for document lifecycle (check-in/check-out, versioning, retention).

**Sources:**
- [IBM BAW Case Management Overview](https://www.ibm.com/docs/en/baw/23.0.x?topic=overview-case-management)
- [IBM BAW Product Overview](https://www.ibm.com/docs/en/baw/23.0.x?topic=2302-product-overview)
- [IBM BAW Case Management (24.x)](https://www.ibm.com/docs/en/baw/24.0.x?topic=24x-case-management)

### Domain Nouns
Rule, Decision Table, Rule Flow, Ruleset, Case, Activity, Stage, Document, Property, Role, Work Item, Approval.

### Policy Shapes
- **Eligibility determination**: compound conditions → eligible/ineligible
- **Threshold-based escalation**: amount > threshold → different handler
- **Parameterized policies**: configurable limits without code changes
- **Sequential validation**: ordered rule execution for multi-step verification
- **Document-gated progression**: cannot advance stage without required documents

### Precept Fit Assessment
- ✅ **Excellent fit**: ODM's action rules and decision tables map directly to Precept guards and constraints. The compound condition → outcome pattern is exactly what Precept guards express.
- ✅ **Good fit**: BAW case lifecycle (stages → activities) maps to Precept states → events.
- ❌ **Bad fit**: ODM rule flows with complex execution ordering (Precept evaluates guards declaratively, not procedurally). FileNet document management. BAW's parallel approval with quorum (multi-actor coordination).

---

## G. Guidewire

### Insurance Lifecycle Shapes

Guidewire is THE dominant insurance-industry platform. Its three modules map to the three core insurance entity lifecycles:

**PolicyCenter — Policy Lifecycle:**
- Draft → Submitted → Quoted → Underwriting → Approved → Issued/Bound → Active → Cancelled/Expired
- Key events: Submit, Quote, Approve, Issue, Endorse (mid-term change), Renew, Cancel, Reinstate
- Guards: Underwriting rules, risk assessment, product eligibility, territory restrictions
- The **endorsement** pattern (modify an active policy mid-term) is distinctive — the policy returns to an "under review" state then reactivates

**ClaimCenter — Claim Lifecycle:**
- Open (FNOL) → Assigned → Investigation → Evaluation → Negotiation → Settlement → Closed/Denied
- Key events: Report (FNOL), Assign, Investigate, Evaluate, Settle, Deny, Reopen, Close
- Guards: Coverage verification, reserve limits, authority levels, fraud indicators
- **Subrogation** and **salvage** are sub-lifecycles within the claim

**BillingCenter — Billing Lifecycle:**
- Invoice Generated → Payment Due → Paid / Overdue → Collections → Written-off / Closed
- Commission calculation and disbursement
- Delinquency tracking with escalation

**Sources:**
- [Guidewire Tutorial (TechSolidity)](https://techsolidity.com/blog/guidewire-tutorial)
- [Guidewire BillingCenter](https://www.guidewire.com/products/core-products/insurancesuite/billingcenter-insurance-billing-software)
- [Guidewire ClaimCenter Guide (uDemand)](https://udemand.org/guidewire-claim-center/)
- [Guidewire ClaimCenter Training (FiestTech)](https://www.fiesttech.com/course/guidewire-claim-center)
- [What is Guidewire (Hillstone)](https://www.hillstone-software.com/what-is-guidewire-software/)
- [Guidewire PDF (Koenig)](https://www.koenig-solutions.com/CourseContent/custom/2025415634-Guidewire.pdf)

### Rule/Eligibility Patterns
- **Underwriting rules**: Risk factors (age, location, claims history, property type) → eligibility, premium tier, required inspections
- **Coverage eligibility**: Policy type + geography + property characteristics → available coverages
- **Authority levels**: Claim value + adjuster level → settlement authority
- **Reserve rules**: Claim type + severity → initial reserve amount
- **Fraud indicators**: Claim timing, history, amount patterns → fraud flag
- **Renewal eligibility**: Claims count, payment history → auto-renew vs. non-renew

### Precept Fit Assessment
- ✅ **Excellent fit**: Insurance claim lifecycle is the #1 canonical case. Policy lifecycle with endorsement loop. Billing delinquency lifecycle.
- ✅ **Excellent fit**: Underwriting rules as guards. Authority levels as threshold guards. Eligibility determination.
- ❌ **Bad fit**: Multi-policy/multi-claim portfolio management. Cross-entity billing integration (policy ↔ billing ↔ claim). Reinsurance cascades. Commission distribution across agents.

---

## H. Temporal

### Workflow Patterns

Temporal models workflows as durable, long-running code. Its canonical patterns:

**1. Entity Lifecycle (Actor Pattern)** — GOOD PRECEPT FIT
- Each entity instance (Account, Subscription, Order) is a dedicated long-running workflow
- The workflow persists state and enforces invariants across the entity's entire life
- Example: Subscription lifecycle — Trial → Active → Overdue → Suspended → Cancelled
- Signals represent external events; queries represent state inspection

**2. Subscription Billing** — PARTIAL FIT
- Each subscription = one workflow
- Timed billing cycles, retry on failure, dunning escalation
- The lifecycle aspect fits; the timer/scheduling aspect doesn't (Precept has no time)

**3. Order Processing (Saga Pattern)** — BAD FIT
- Multi-service orchestration: Reserve Inventory → Charge Payment → Ship → Notify
- Compensation logic on failure (release inventory, refund payment)
- This is ORCHESTRATION across multiple entities/services

**4. Approval Flows (Human-in-the-Loop)** — GOOD FIT
- Workflow waits for approval signal with timeout and escalation
- Maps to Precept's event-with-guard pattern (Approve event fires only when authorized)

**Sources:**
- [Temporal Workflow Documentation](https://docs.temporal.io/workflows)
- [Temporal Workflow Patterns (Keith Tenzer)](https://keithtenzer.com/temporal/Temporal_Fundamentals_Workflow_Patterns/)
- [Saga Pattern (microservices.io)](https://microservices.io/patterns/data/saga.html)
- [Saga Orchestration (AWS)](https://docs.aws.amazon.com/prescriptive-guidance/latest/agentic-ai-patterns/saga-orchestration-patterns.html)
- [Orchestration vs Choreography (GeeksforGeeks)](https://www.geeksforgeeks.org/system-design/orchestration-vs-choreography/)

### Domain Nouns
Workflow, Activity, Signal, Query, Timer, Saga, Compensation, Worker.

### Precept Fit Assessment
- ✅ **Good fit**: Entity-as-actor pattern (one entity, explicit states, events via signals). Subscription lifecycle. Approval workflow.
- ❌ **Bad fit**: Saga pattern (multi-service compensation). Timer-based scheduling. Activity fan-out/fan-in. Event-driven choreography across services.

---

## Cross-Platform Synthesis

### 1. Recurring Lifecycle Shapes (Appear in 3+ Platforms)

**Shape 1: Intake → Review → Decision → Fulfillment → Close**
- Platforms: ALL (Salesforce, ServiceNow, Pega, Appian, Camunda, IBM, Guidewire, Temporal)
- Examples: Service request, benefit application, permit, loan application
- This is the "universal case lifecycle" — every platform's most basic pattern

**Shape 2: Intake → Evidence/Documentation Loop → Adjudication → Settlement/Denial**
- Platforms: Salesforce Industries, Pega, Guidewire, Appian, IBM BAW
- Examples: Insurance claim, healthcare prior authorization, fraud investigation
- Distinctive feature: the **evidence loop** — entity bounces between "needs more info" and "under review" states before a decision can be made
- **Precept already has this** in `insurance-claim.precept`

**Shape 3: Submission → Risk Assessment → Approval Gate → Active → Renewal/Cancellation**
- Platforms: Salesforce Industries, Pega, Guidewire, IBM ODM
- Examples: Insurance policy, loan, membership, contract
- Distinctive feature: the entity lives in an "Active" state for an extended period, with periodic re-evaluation (renewal) and mid-term modification (endorsement)
- **Partially covered** by `loan-application.precept` but missing the post-approval "Active" phase

**Shape 4: New → Assess → Authorize → Execute → Review → Close (Approval-Gated Execution)**
- Platforms: ServiceNow (Change Management), Pega, Appian, IBM BAW
- Examples: Change request, procurement request, contract modification, regulatory submission
- Distinctive feature: explicit **authorization gate** between assessment and execution, often with different approval paths based on risk/type classification
- **Not well-represented** in current samples

**Shape 5: Detected → Triaged → Investigation → Containment → Remediation → Verified → Closed**
- Platforms: ServiceNow (Security Ops), Pega, Appian
- Examples: Security incident, compliance violation, fraud case, quality defect
- Distinctive feature: **containment** as a distinct phase before full resolution; investigation may loop
- **Not represented** in current samples

**Shape 6: Application → Eligibility Determination → Approved/Denied → Appeal → Re-review → Final Decision**
- Platforms: Appian (public sector), Salesforce Industries (healthcare), Pega (healthcare/financial)
- Examples: Benefits application, permit application, prior authorization, disability claim
- Distinctive feature: **appeal loop** that revisits the decision with additional evidence
- **Not represented** in current samples

**Shape 7: Draft → Submitted → Under Review → Approved → Active → Amended → Active → Expired/Terminated**
- Platforms: Salesforce, Guidewire (PolicyCenter), Pega, IBM BAW
- Examples: Insurance policy, contract, agreement, regulatory filing
- Distinctive feature: **amendment/endorsement loop** — entity returns to review from Active state, then reactivates
- **Not represented** in current samples

**Shape 8: Request → Fulfillment → Confirmation → Closed (Simple Service Fulfillment)**
- Platforms: ServiceNow (Service Request), Salesforce (Service Cloud), Pega
- Examples: Equipment request, access request, information request
- Distinctive feature: minimal gates, focus on fulfillment tracking rather than complex decision logic
- **Partially covered** by `building-access-badge-request.precept`

### 2. Recurring Policy Shapes (Cross-Platform)

| Policy Pattern | Platforms | Precept Mapping |
|---------------|-----------|-----------------|
| **Eligibility determination** (multi-factor → eligible/ineligible) | ALL | Guard expression with compound conditions |
| **Approval threshold** (amount/risk → approval level required) | Salesforce, ServiceNow, Pega, Appian, IBM | Guard: `amount > threshold` on Approve event |
| **SLA enforcement** (time in state → escalation) | ServiceNow, Salesforce, Pega, Appian | Constraint on state (Precept would need timer support for full fidelity; currently modeled as a numeric field) |
| **Priority/severity matrix** (impact × urgency → priority) | ServiceNow, Pega, IBM ODM | Computed field or guard expression |
| **Document completeness gate** (required docs present → can proceed) | Salesforce, Pega, Appian, Guidewire, IBM BAW | Guard: `hasDocument1 and hasDocument2` or set-based `.count >= requiredCount` |
| **Risk classification** (factors → Low/Medium/High) | Guidewire, Pega, IBM ODM, Camunda DMN | Guard or constraint expression |
| **Routing/assignment rules** (type + priority → handler) | ServiceNow, Salesforce, Pega | Field assignment in event action (`set assignedTeam = ...`) |
| **Authority level** (claim/request value → who can approve) | Guidewire, ServiceNow, Pega | Guard: `amount <= authorityLimit` |
| **Parameterized thresholds** (configurable limits) | IBM ODM, Camunda | Field-based thresholds that guards reference |
| **Escalation triggers** (condition met → force state change) | ServiceNow, Salesforce, Pega | Modeled as an Escalate event with appropriate guards |

### 3. Recurring Domain Nouns (Cross-Platform)

**Tier 1 — Universal (appear in 5+ platforms):**
- Case, Claim, Request, Application, Incident, Policy, Order

**Tier 2 — Common (appear in 3-4 platforms):**
- Contract, Ticket, Invoice, Approval, Investigation, Matter, Enrollment, Submission

**Tier 3 — Domain-Specific (appear in 1-2 platforms but represent important patterns):**
- Change Request (ITSM), Prior Authorization (Healthcare), Endorsement (Insurance), Permit (Government), Vulnerability (Security), Work Order (Facilities), Benefit (Social Services)

---

## Gap Analysis

### What Precept's Current 21 Samples Cover Well

| Pattern | Sample(s) |
|---------|-----------|
| Evidence-loop claim | `insurance-claim.precept` |
| Intake → Review → Decision | `loan-application.precept`, `apartment-rental-application.precept` |
| Simple service fulfillment | `building-access-badge-request.precept`, `refund-request.precept` |
| Helpdesk/ticket lifecycle | `it-helpdesk-ticket.precept` |
| Hiring/pipeline | `hiring-pipeline.precept` |
| Scheduling | `clinic-appointment-scheduling.precept`, `vehicle-service-appointment.precept` |
| Physical-world lifecycle | `trafficlight.precept`, `crosswalk-signal.precept`, `parcel-locker-pickup.precept` |
| Library domain | `library-book-checkout.precept`, `library-hold-request.precept` |
| Subscription/retention | `subscription-cancellation-retention.precept` |
| Reimbursement/expense | `travel-reimbursement.precept` |
| Work order | `maintenance-work-order.precept` |
| Warranty | `warranty-repair-request.precept` |
| Registration | `event-registration.precept` |
| Utility/operations | `utility-outage-report.precept` |
| Restaurant operations | `restaurant-waitlist.precept` |

### What's Missing — High-Priority Gaps

These are patterns that appear across 3+ enterprise platforms and represent significant real-world domains that current samples don't cover:

**1. Approval-Gated Execution (Change Request pattern)**
- The ServiceNow Change Management lifecycle is THE canonical example: New → Assess → Authorize → Scheduled → Implement → Review → Closed
- With type-based branching (Normal/Standard/Emergency)
- No current sample has this shape — the key missing pattern is the **authorization gate between assessment and execution**
- **Suggested sample**: `change-request.precept` or `procurement-request.precept`

**2. Security/Compliance Incident with Containment**
- Detected → Triaged → Investigation → Containment → Remediation → Verified → Closed
- Distinctive because containment is a separate phase from resolution
- Investigation may loop (re-open investigation from containment)
- **Suggested sample**: `security-incident.precept` or `compliance-violation.precept`

**3. Appeal-Loop Decision (Benefits/Entitlement)**
- Application → Eligibility → Approved/Denied → Appeal → Re-review → Final Decision
- The appeal loop is a defining pattern in public-sector/healthcare/regulated domains
- Current samples don't have a "denied then appealed" path
- **Suggested sample**: `benefits-application.precept` or `prior-authorization.precept`

**4. Long-Lived Contract/Policy with Amendment**
- Draft → Review → Active → Amended → Active → Renewed/Expired/Terminated
- The policy/contract lives in "Active" for an extended period with mid-term modifications
- `loan-application.precept` stops at approval — doesn't model the post-approval active life
- **Suggested sample**: `service-contract.precept` or `insurance-policy-lifecycle.precept`

**5. KYC/Identity Verification Onboarding**
- Data Collection → Identity Verification → Risk Assessment → Enhanced Due Diligence (conditional) → Approved → Active
- Conditional escalation based on risk tier
- Every financial services platform (Salesforce, Pega, IBM) showcases this
- **Suggested sample**: `customer-onboarding-kyc.precept`

**6. Regulatory Submission/Permit**
- Application → Completeness Check → Technical Review → Public Comment (optional) → Decision → Issuance/Denial → Appeal
- Document-completeness gates
- Very prominent in Appian public-sector case studies
- **Suggested sample**: `building-permit-application.precept` or `regulatory-submission.precept`

**7. Invoice/Payment Lifecycle**
- Generated → Sent → Due → Paid/Overdue → Collections → Written-off/Closed
- Guidewire BillingCenter, Salesforce, ServiceNow all model this
- Delinquency escalation, partial payment handling
- **Suggested sample**: `invoice.precept`

**8. Problem/Root-Cause Investigation**
- Reported → Confirmed → Root Cause Analysis → Workaround Documented → Fix Identified → Fix Verified → Closed
- ServiceNow Problem Management's distinctive "investigate → document known error → fix" flow
- Different from an incident (which is about restoration) — this is about investigation
- **Suggested sample**: `problem-investigation.precept`

### Lower-Priority Gaps (interesting but less universal)

- **Prior Authorization** (healthcare utilization management — specialized but huge market)
- **Underwriting Case** (risk assessment as a standalone lifecycle, not embedded in policy)
- **Employee Offboarding** (HR — cross-departmental but the offboarding entity itself has a lifecycle)
- **Vulnerability Management** (Security — Identified → Assessed → Remediation → Verified)
- **Legal Matter** (intake → triage → assignment → work → closure)
- **Subscription Billing Cycle** (extends `subscription-cancellation-retention.precept` with billing/dunning)

---

## Anti-Patterns

### What Does NOT Translate to Precept

These patterns appear prominently across enterprise platforms but violate Precept's single-entity, single-lifecycle model:

**1. Multi-Entity Orchestration**
- Pega parent-case/child-case hierarchies
- Temporal Saga pattern (Order → Inventory → Payment → Shipping as separate services)
- ServiceNow cross-ticket linking (Problem → spawns Incidents)
- Guidewire Policy ↔ Claim ↔ Billing integration
- **Why not**: Precept models ONE entity. Coordinating multiple entities is orchestration.

**2. Multi-Actor Choreography**
- BPMN swimlane processes with multiple participants
- Parallel approval with quorum rules ("3 of 5 must approve")
- Appian cross-departmental onboarding (IT + Facilities + Security + HR)
- **Why not**: Precept doesn't model role-based routing or multi-party coordination. A single approver role can be represented in guards, but multi-party consensus cannot.

**3. Timer/Schedule-Driven Automation**
- ServiceNow SLA timers that auto-escalate
- Temporal timer-based billing cycles
- Guidewire renewal processing on anniversary dates
- BPMN timer intermediate events
- **Why not**: Precept has no time dimension. SLA-like behavior can be approximated with numeric fields (e.g., `daysOpen`), but actual timer-driven state transitions are out of scope.

**4. Data Pipelines and ETL**
- IBM ODM batch rule execution over datasets
- Salesforce data migration flows
- **Why not**: Precept operates on single instances, not collections of entities.

**5. AI/ML Decisioning**
- Pega Next-Best-Action (AI model output drives decisions)
- Salesforce Einstein recommendations
- **Why not**: Precept guards are deterministic expressions, not model inference.

**6. Entity Transformation (Type Change)**
- Salesforce Lead → Opportunity conversion
- Application → Policy issuance (if modeled as creating a NEW policy entity from an application entity)
- **Why not**: Precept models one entity type. An entity cannot become a different entity.

**7. Complex Subprocess Hierarchies**
- BPMN embedded/call subprocesses
- Pega nested process flows within stages
- **Why not**: Precept's state model is flat (states, not nested state hierarchies). Substates could be modeled as top-level states, but deep nesting suggests orchestration.

---

## Summary: Top Recommendations for Sample Corpus

### Highest-Impact New Samples (ordered by cross-platform evidence strength)

1. **Change Request** — The most thoroughly documented lifecycle in enterprise IT. ServiceNow's state model is publicly specified. Models the approval-gated-execution shape that no current sample covers.

2. **Benefits/Entitlement Application** — Appeal-loop pattern from Appian/Pega/Salesforce public sector. The most distinctive lifecycle shape that's completely missing.

3. **Insurance Policy Lifecycle** — The endorsement/amendment loop from Guidewire/Salesforce/Pega. Extends the existing `insurance-claim.precept` into the complementary policy domain.

4. **Security Incident** — Containment-before-remediation pattern from ServiceNow/Pega/Appian. Fills a domain gap (cybersecurity) with a distinctive lifecycle shape.

5. **KYC/Customer Onboarding** — Risk-tiered verification escalation from Pega/Salesforce/IBM. Every financial services platform showcases this.

6. **Invoice/Billing** — Simple but universal lifecycle from Guidewire/ServiceNow/Salesforce. Delinquency escalation is a clean guard pattern.

7. **Regulatory Submission/Permit** — Document-completeness-gated review from Appian public sector. Strong guard/constraint pattern.

8. **Problem Investigation** — Root-cause-analysis lifecycle from ServiceNow. Distinguishes investigation-focused lifecycles from incident-restoration lifecycles.

### Existing Samples to Consider Extending

- `insurance-claim.precept` → Consider adding fraud-indicator guards (Guidewire pattern)
- `loan-application.precept` → Consider extending past approval into active/servicing phase (Salesforce/Pega pattern)
- `subscription-cancellation-retention.precept` → Consider adding billing/dunning cycle (Temporal/Guidewire pattern)
- `it-helpdesk-ticket.precept` → Consider aligning more closely with ServiceNow Incident state model
