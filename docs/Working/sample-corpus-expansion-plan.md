# Precept Sample Corpus Expansion Plan
## Workflow Governance Edition

## Status (Updated 2026-05-25)

**Samples completed so far:** 32 new numbered samples (01-32) authored to date; see status table below for individual file states.  
**Samples in progress:** 0  
**Remaining samples:** the original §1.x detailed specs largely did not ship under their planned filenames — see **Phase 1 Implementation Drift** subsection inside Phase 1 for what shipped, what didn't, and what is now deferred. Phase 5 (catalog coverage showcase) added 2026-05-25 with 4 sample specs targeting unshowcased catalog members.  

### Completed Samples (18 files)

| # | File | Domain | Status |
|---|------|--------|--------|
| 01 | medical-prior-auth.precept | Healthcare (FHIR) | ✅ Compiled |
| 02 | prior-auth-appeal.precept | Healthcare Appeals | ✅ Compiled |
| 03 | patient-care-plan-coordination.precept | Healthcare Care Coord | ✅ Compiled |
| 04 | prescription-refill-request.precept | Healthcare Pharmacy | ✅ Compiled |
| 05 | lab-test-order-results.precept | Healthcare Diagnostics | ✅ Compiled |
| 06 | shopping-cart.precept | E-commerce | ✅ 0 errors, 5/5 proofs |
| 07 | subscription-cancellation-retention.precept | SaaS Billing | ✅ 0 errors, 2/2 proofs |
| 08 | insurance-claim-processing.precept | Insurance | ✅ 0 errors, 10/10 proofs |
| 09 | apartment-rental-application.precept | Real Estate | ✅ 0 errors, 2/2 proofs |
| 10 | production-order-tracking.precept | Manufacturing | ✅ 0 errors, 5/5 proofs |
| 11 | insurance-claim-adjudication.precept | Insurance (ACORD) | ✅ 0 errors, 1/1 proofs |
| 12 | insurance-policy-endorsement.precept | Insurance (ACORD) | ✅ 0 errors, 1/1 proofs |
| 13 | insurance-renewal-processing.precept | Insurance (ACORD) | ✅ 0 errors, 1/1 proofs |
| 14 | insurance-underwriting.precept | Insurance (ACORD) | ✅ 0 errors, 1/1 proofs |
| 15 | insurance-subrogation.precept | Insurance (ACORD) | ✅ 0 errors, 5/5 proofs |
| 16 | supplier-quality-management.precept | Manufacturing/SQM | ✅ 0 errors, 5/5 proofs |
| 17 | supplier-corrective-action.precept | Manufacturing/SCAR | ✅ 0 errors, 6/6 ensures |
| 18 | equipment-downtime-tracking.precept | Manufacturing/OEE | ✅ 0 errors, 5/5 ensures |

## Executive Summary

Original current sample count: **30 precept files**  
Target sample count: **~60 precept files** (double the corpus)  
**Strategic pivot:** From technical feature coverage to **workflow governance demonstration**

### Why Workflow Governance?

Precept's unique value proposition is **binding state machines + data rules into a single enforceable contract**. While other tools govern data (validation libraries) or lifecycle (state machines), only Precept combines them. This pivot aligns the sample corpus with that strategic differentiation.

**Key shift:**
- **Old approach:** 30 samples organized by technical feature gaps (Queue, Stack, Lookup, Temporal, etc.)
- **New approach:** Industry-grade workflow samples grounded in 80+ research documents covering FHIR, ACORD, ISO 20022, GDPR, and enterprise patterns

**Research foundation:** This plan leverages research from:
- `research/language/expressiveness/type-system-domain-survey.md` — 10-domain field analysis (100 fields across insurance, clinical trials, SaaS billing, manufacturing, etc.)
- `research/language/expressiveness/temporal-type-strategy.md` — NodaTime-aligned temporal patterns
- `research/language/expressiveness/sample-temporal-pattern-catalog.md` — 91 FUTURE(date) markers, 29 integer surrogates across 15 samples
- `research/product/entity-governance-landscape.md` — Salesforce, ServiceNow, Guidewire analogs
- Industry standards: FHIR (150+ resources), ACORD insurance standards, ISO 20022 financial messaging

**Goal:** Demonstrate Precept as the **library-weight alternative to enterprise platforms** (Salesforce, ServiceNow, Guidewire) for teams building in .NET.

---

## Current Sample Inventory & Gap Analysis

### Existing Samples (30 files) - Categorized by Strategic Value

#### Tier 1: Strong Workflow Governance (8 samples)
These samples demonstrate Precept's core value: state machines + data rules bound together.

1. **insurance-claim.precept** - Claims processing with fraud detection (ACORD-aligned)
2. **loan-application.precept** - Loan underwriting workflow (Pattern A)
3. **apartment-rental-application.precept** - Rental workflow with approval states
4. **hiring-pipeline.precept** - Recruitment workflow with multi-stage review
5. **insurance-policy-lifecycle.precept** - Policy states, renewal, lapse (ACORD-aligned)
6. **library-book-checkout.precept** - Circulation with temporal overdue logic
7. **maintenance-work-order.precept** - Work order lifecycle with duration tracking
8. **warranty-repair-request.precept** - Warranty claims workflow

#### Tier 2: Moderate Workflow (10 samples)
These have state machines but lighter data governance complexity.

9. **building-access-badge-request.precept** - Access control workflow
10. **clinic-appointment-scheduling.precept** - Healthcare scheduling (FHIR-aligned)
11. **event-registration.precept** - Event registration with payment
12. **it-helpdesk-ticket.precept** - IT support workflow with SLA tracking
13. **library-hold-request.precept** - Library holds with temporal logic
14. **parcel-locker-pickup.precept** - Package pickup workflow
15. **payment-method.precept** - Payment method validation
16. **refund-request.precept** - Refund processing
17. **travel-reimbursement.precept** - Expense reimbursement workflow
18. **vehicle-service-appointment.precept** - Service scheduling

#### Tier 3: Stateless / Data-Only (6 samples)
These demonstrate data governance without lifecycle — de-emphasized in new strategy.

19. **computed-tax-net.precept** - Computed fields only
20. **crosswalk-signal.precept** - Traffic control (simplified state machine)
21. **customer-profile.precept** - Customer data governance (no lifecycle)
22. **fee-schedule.precept** - Stateless pricing configuration
23. **inventory-item.precept** - Complex dimensional analysis (no lifecycle)
24. **invoice-line-item.precept** - Invoice line validation

#### Tier 4: Edge Cases / Technical Demos (6 samples)
These demonstrate specific language features or edge cases.

25. **sum-on-rhs-rule.precept** - Rule with sum aggregation
26. **transitive-ordering.precept** - Ordering/transitivity rules
27. **trafficlight.precept** - Traffic light with emergency mode
28. **utility-outage-report.precept** - Outage tracking
29. **restaurant-waitlist.precept** - Restaurant seating
30. **subscription-cancellation-retention.precept** - Subscription retention (compact)

### Strategic Gap Analysis

**8 Industry Domains with ZERO representation:**
1. **Healthcare (FHIR)** — Only clinic-appointment-scheduling has minimal FHIR alignment. Missing: Prior Authorization, Care Plan, Referral workflows
2. **Insurance (ACORD)** — Only basic claim/policy. Missing: Underwriting, Policy Administration, Endorsement workflows
3. **Finance (ISO 20022)** — No payment initiation, portfolio management, or trade settlement workflows
4. **Manufacturing** — Only maintenance work order. Missing: Quality Control, Production Scheduling, Supply Chain workflows
5. **Regulatory Compliance** — No GDPR, PCI-DSS, SOC2, or FDA 21 CFR Part 11 workflows
6. **SaaS/Subscription** — Only basic cancellation. Missing: Billing cycles, Usage metering, Upgrade/downgrade workflows
7. **Legal** — No matter management, discovery, or contract lifecycle workflows
8. **Real Estate** — No transaction, escrow, or lease management workflows

**Research-backed field patterns missing:**
- From `type-system-domain-survey.md`: 41 choice fields, 30 date fields, 16 decimal fields, 11 integer fields across 10 domains
- From `sample-temporal-pattern-catalog.md`: 70+ pure calendar date fields, 10 period fields, 9 duration fields requiring native temporal types

---

## Implementation Strategy: Sequential with Review

**Owner:** Frank (Lead/Architect & Language Designer)  
**Reviewer:** Steinbrenner (PM/Research - validates realism against industry standards)  
**Approach:** One sample at a time, with review cycle

---

## Implementation Strategy: Sequential One-Agent-at-a-Time

**Owner:** Frank (Lead/Architect & Language Designer)  
**Reviewer 1:** Steinbrenner (PM/Research - validates realism against industry standards)  
**Reviewer 2:** George (Runtime Dev - validates technical correctness and DSL pipeline)  
**Approach:** One sample at a time, fully sequential workflow - no parallel execution

### Per-Sample Workflow (Sequential Iterative Loop)

```
┌─────────────────────────────────────────────────────────────────┐
│  SAMPLE N: [Sample Name]                                       │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│  STEP 1: FRANK - DESIGN & AUTHOR                               │
│  ─────────────────────────────────────────────────────────────  │
│  • Read research documents (FHIR, ACORD, domain surveys)       │
│  • Design state machine and field structure                    │
│  • Author .precept file                                        │
│  • MCP TOOL VALIDATION LOOP:                                   │
│    - precept_quickstart — orient to capabilities               │
│    - precept_syntax — verify syntax for constructs             │
│    - precept_types — confirm field types and modifiers         │
│    - precept_operations — validate operators for types         │
│    - precept_domains — lookup currencies, units, dimensions    │
│    - precept_compile — COMPILE after each major section        │
│    - precept_diagnostic — look up any diagnostic codes         │
│  • Iterate until sample compiles cleanly (0 unexpected errors) │
│  • SUBMIT to Steinbrenner for review                           │
│                                                                 │
│  ↓ WAIT FOR STEINBRENNER REVIEW ↓                              │
│                                                                 │
│  STEP 2: STEINBRENNER - REALISM REVIEW                         │
│  ─────────────────────────────────────────────────────────────  │
│  Review against industry standards (FHIR, ACORD, ISO):         │
│  • Field completeness against research documents               │
│  • State coverage matching real workflows                      │
│  • Temporal accuracy and realistic timeframes                  │
│  • Business logic matches industry practices                   │
│  • Edge cases and error conditions covered                     │
│  • Complexity appropriate (not trivial, not unwieldy)          │
│                                                                 │
│  OUTPUT: ✅ Approved | ⚠️ Minor Feedback | ❌ Needs Revision  │
│  • If approved → SUBMIT to George for technical review         │
│  • If feedback → RETURN to Frank for revision                  │
│                                                                 │
│  ↓ WAIT FOR GEORGE REVIEW ↓                                    │
│                                                                 │
│  STEP 3: GEORGE - TECHNICAL REVIEW                             │
│  ─────────────────────────────────────────────────────────────  │
│  Validate DSL pipeline correctness:                            │
│  • Parser accepts syntax without errors                        │
│  • Type checker performs correct validations                   │
│  • Expression semantics are sound                              │
│  • State graph is well-formed (no dead ends, unreachable)      │
│  • Diagnostic codes are accurate and actionable                │
│  • Runtime behavior matches specification                      │
│  • Catalog-driven design (no hardcoded token sets)             │
│                                                                 │
│  OUTPUT: ✅ Approved | ⚠️ Minor Feedback | ❌ Needs Revision  │
│  • If approved → SUBMIT to Quality Gate                        │
│  • If feedback → RETURN to Frank for revision                  │
│                                                                 │
│  ↓ WAIT FOR QUALITY GATE ↓                                     │
│                                                                 │
│  STEP 4: QUALITY GATE                                          │
│  ─────────────────────────────────────────────────────────────  │
│  • Verify against expansion plan requirements                  │
│  • Confirm language feature coverage                           │
│  • MCP TOOL FINAL VALIDATION:                                  │
│    - precept_compile — confirm clean compilation               │
│    - precept_inspect — verify structure and metadata           │
│  • Document sample characteristics                             │
│  • Record language features demonstrated                       │
│  • Add to sample index with description                        │
│                                                                 │
│  ✅ SAMPLE COMPLETE → NEXT SAMPLE                              │
└─────────────────────────────────────────────────────────────────┘
```

### Sequential Execution Rules

**CRITICAL: Only ONE agent active at a time**

1. **Frank authors Sample 1** → completes → submits for review
2. **Steinbrenner reviews Sample 1** → completes → submits to George or returns to Frank
3. **George reviews Sample 1** → completes → submits to Quality Gate or returns to Frank
4. **Quality Gate validates Sample 1** → completes → Sample 1 is DONE
5. **Frank begins Sample 2** → repeats full cycle
6. Continue sequentially through all 35 samples

**Iteration Rules:**

**When Steinbrenner or George provide feedback:**

1. **Minor Feedback (⚠️):**
   - Frank incorporates changes
   - Re-compile with MCP tools
   - Quick re-review by the reviewer who provided feedback
   - If approved, proceed to next step

2. **Needs Revision (❌):**
   - Frank revises sample substantially
   - Re-compile with MCP tools until clean
   - Full re-review by both Steinbrenner AND George
   - If still rejected after 2 iterations, escalate to Shane for guidance

**MCP Tool Requirements for Frank:**

Frank **MUST** use MCP tools at these checkpoints:
- **Before writing:** `precept_quickstart`, `precept_syntax`, `precept_types`
- **During writing:** `precept_compile` after each state/event/rule section
- **On errors:** `precept_diagnostic` for any diagnostic code encountered
- **Before submission:** `precept_compile` final clean compile, `precept_inspect` for structure validation

**Documentation Requirements:**

Each sample must document:
- Which MCP tools were used and what was learned
- Which language features are demonstrated
- Industry standard alignment (FHIR resources, ACORD standards, etc.)
- Research document references
- Any edge cases or unusual patterns

---

## Phase 1: Healthcare & Insurance Workflows (15 samples)

### Healthcare Domain (10 samples)

**Sample 1: Medical Prior Authorization**
- **Domain:** Healthcare (FHIR-aligned)
- **FHIR Resources:** Patient, ServiceRequest, Coverage, ExplanationOfBenefit, CoverageEligibilityResponse
- **Key Features:** Multi-stage approval, clinical criteria validation, temporal SLA tracking, prior auth history
- **Fields:** Patient ID, diagnosis codes (ICD-10), procedure codes (CPT), authorization dates, coverage limits, clinical notes, authorization number
- **States:** Submitted → Under Review → Clinical Review → Payer Review → Approved/Denied/Escalated
- **Research Reference:** `type-system-domain-survey.md` (Healthcare fields), FHIR Prior Authorization specification
- **Demonstrates:** Complex workflow with temporal constraints, cross-field validation, industry-standard data model, multi-stage approval
- **Reviewer Focus:** FHIR resource alignment, clinical workflow accuracy, SLA timeframes, denial reasons

**Sample 2: Prior Authorization Appeal**
- **Domain:** Healthcare Appeals (FHIR-aligned)
- **FHIR Resources:** CarePlan, DocumentReference, Task
- **Key Features:** Appeal submission, additional documentation, clinical review, appeal outcome
- **Fields:** Original auth ID, appeal reason, new clinical evidence, appeal date, outcome, effective date
- **States:** Filed → Under Review → Clinical Review → Decision Made → Approved/Denied
- **Research Reference:** FHIR Appeal resource, prior authorization appeal workflows
- **Demonstrates:** Multi-level review processes, document attachments, temporal deadlines, conditional routing
- **Reviewer Focus:** Appeal timeline requirements, evidence submission rules, decision criteria

**Sample 3: Patient Care Plan Coordination** ✓ COMPLETED
- **Domain:** Healthcare Care Coordination (FHIR-aligned)
- **FHIR Resources:** CarePlan, Condition, Encounter, Task, Goal
- **Key Features:** Care plan creation, goal tracking, task assignment, progress monitoring, care team collaboration
- **Fields:** Patient ID, care plan goals, assigned tasks, target dates, progress notes, completion status, care team members
- **States:** New → Draft → Active → In Progress → Milestone Review → Completed/Discontinued
- **Research Reference:** FHIR CarePlan resource, care coordination patterns
- **Demonstrates:** Goal tracking workflows, task assignments, temporal progress tracking, multi-party collaboration
- **Status:** Authored 2026-05-20, compiles with warnings (state reachability)
- **Reviewer Focus:** Care plan structure, goal measurability, task dependencies, progress measurement
- **Note:** Compiles successfully; warnings about unreachable states are design choices

**Sample 4: Prescription Refill Request**
- **Domain:** Healthcare Pharmacy
- **Key Features:** Refill authorization, drug interaction checks, dosage validation, insurance approval, refill history
- **Fields:** Patient ID, medication, dosage, refills remaining, prescriber, pharmacy, approval status, last fill date
- **States:** Requested → Drug Interaction Check → Insurance Review → Approved → Filled/Rejected
- **Research Reference:** Pharmacy workflow standards, drug interaction patterns
- **Demonstrates:** Drug interaction rules, temporal refill windows, multi-factor approval, audit trail
- **Reviewer Focus:** Refill frequency rules, drug interaction logic, insurance workflow, timing constraints

**Sample 5: Lab Test Order & Results**
- **Domain:** Healthcare Diagnostics
- **Key Features:** Test ordering, specimen collection, result reporting, critical value alerts, result verification
- **Fields:** Order ID, patient ID, test codes, collection time, results, reference ranges, critical flags, verified by
- **States:** Ordered → Collected → Processing → Results Entered → Verified → Reviewed
- **Research Reference:** Laboratory workflow standards, critical value reporting requirements
- **Demonstrates:** Temporal workflows, conditional alerts based on values, data validation, verification workflows
- **Reviewer Focus:** Critical value thresholds, turnaround time SLAs, result validation, verification requirements

**Sample 6: Patient Referral Management**
- **Domain:** Healthcare Referrals
- **Key Features:** Specialist referrals, authorization tracking, appointment scheduling, follow-up requirements, referral closure
- **Fields:** Referral ID, referring provider, specialist, diagnosis, authorization status, appointment date, follow-up required, consultation notes
- **States:** Requested → Authorized → Scheduled → Completed → Report Received → Closed
- **Research Reference:** Referral management workflows, authorization requirements
- **Demonstrates:** Multi-party workflows, temporal tracking, conditional routing based on authorization, report integration
- **Reviewer Focus:** Authorization requirements, referral validity periods, follow-up protocols, closure criteria

**Sample 7: Utilization Review Case**
- **Domain:** Healthcare Utilization Management
- **Key Features:** Case management, level of care determination, discharge planning, utilization metrics
- **Fields:** Patient ID, admission date, diagnosis, level of care, discharge date, utilization review notes, authorization status
- **States:** Admitted → Initial Review → Ongoing Review → Discharge Planning → Discharged
- **Research Reference:** Utilization management standards, level of care criteria
- **Demonstrates:** Temporal tracking, complex decision logic, multi-stage reviews, discharge planning
- **Reviewer Focus:** Level of care criteria, review frequency, discharge planning requirements

**Sample 8: Healthcare Prior Authorization (Enhanced)**
- **Domain:** Healthcare Prior Authorization (FHIR-aligned)
- **FHIR Resources:** CarePlan, ServiceRequest, Coverage, ExplanationOfBenefit, CoverageEligibilityResponse
- **Key Features:** Comprehensive prior authorization with clinical criteria, peer-to-peer review, appeal workflow integration
- **Fields:** Authorization ID, patient info, service details, clinical criteria, approval status, peer review notes, appeal options
- **States:** Submitted → Clinical Review → Payer Review → Peer Review → Approved/Denied/Appeal
- **Research Reference:** FHIR Prior Authorization specification, MACRA requirements
- **Demonstrates:** Multi-stage clinical review, peer-to-peer workflows, denial reason codes, appeal triggers
- **Reviewer Focus:** Clinical criteria alignment, peer review requirements, appeal workflow integration

**Sample 9: Patient Enrollment**
- **Domain:** Healthcare Enrollment
- **Key Features:** Patient registration, eligibility verification, insurance enrollment, member ID assignment
- **Fields:** Patient ID, personal information, insurance details, effective date, member ID, enrollment status
- **States:** Application Submitted → Eligibility Verified → Insurance Verified → Enrolled → Active
- **Research Reference:** Healthcare enrollment workflows, eligibility verification standards
- **Demonstrates:** Multi-step verification, temporal effective dating, document collection, status transitions
- **Reviewer Focus:** Enrollment requirements, verification processes, effective dating rules

**Sample 10: Medical Device Tracking**
- **Domain:** Healthcare Medical Devices
- **Key Features:** Device assignment, maintenance tracking, usage monitoring, recall management
- **Fields:** Device ID, patient assignment, prescription, maintenance schedule, usage logs, recall status
- **States:** Available → Assigned → In Use → Maintenance Required → Retired/Recalled
- **Research Reference:** Medical device management standards, FDA recall procedures
- **Demonstrates:** Assignment workflows, temporal maintenance, conditional recalls, audit trails
- **Reviewer Focus:** Device tracking requirements, maintenance schedules, recall procedures

### Insurance Domain (5 samples)

**Sample 11: Insurance Claim Adjudication**
- **Domain:** Insurance Claims (ACORD-aligned)
- **Key Features:** Claim intake, coverage verification, loss calculation, payment processing, subrogation
- **Fields:** Claim ID, policy number, loss date, coverage types, loss amount, deductible, payment amount, subrogation potential
- **States:** Filed → Review → Investigation → Adjudicated → Payment Processing → Paid/Denied/Closed
- **Research Reference:** ACORD claim standards, claims adjudication workflows
- **Demonstrates:** Complex calculations, coverage rules, multi-stage approval workflows, subrogation logic
- **Reviewer Focus:** Coverage verification logic, loss calculation accuracy, denial reasons, subrogation criteria

**Sample 12: Insurance Policy Endorsement**
- **Domain:** Insurance Policy Administration (ACORD-aligned)
- **Key Features:** Policy modification, premium adjustment, effective dating, approval workflow, endorsement history
- **Fields:** Policy number, endorsement type, effective date, premium change, reason, approval status, prior terms
- **States:** Requested → Under Review → Approved → Pending Effective Date → Issued/Rejected
- **Research Reference:** ACORD endorsement standards, policy change workflows
- **Demonstrates:** Effective dating, premium calculations, conditional approval workflows, historical tracking
- **Reviewer Focus:** Effective dating rules, premium calculation accuracy, endorsement types, approval criteria

**Sample 13: Insurance Renewal Processing**
- **Domain:** Insurance Policy Lifecycle (ACORD-aligned)
- **Key Features:** Renewal calculation, risk reassessment, premium adjustment, renewal notice, non-renewal handling
- **Fields:** Policy number, renewal date, risk factors, premium change, renewal status, notice sent, renewal terms
- **States:** Up for Renewal → Risk Review → Quote Generated → Notice Sent → Renewed/Lapsed/Cancelled
- **Research Reference:** Policy renewal workflows, risk reassessment patterns
- **Demonstrates:** Temporal workflows, risk-based recalculations, conditional outcomes, notice requirements
- **Reviewer Focus:** Renewal timing, risk factor evaluation, lapse/cancellation rules, notice periods

**Sample 14: Insurance Underwriting**
- **Domain:** Insurance Underwriting (ACORD-aligned)
- **Key Features:** Risk assessment, application review, coverage determination, premium rating, binding
- **Fields:** Application ID, applicant info, risk factors, coverage requested, premium quote, underwriting decision
- **States:** Received → Information Gathering → Risk Assessment → Quote Generated → Approved/Declined/Modified
- **Research Reference:** ACORD underwriting standards, risk assessment methodologies
- **Demonstrates:** Risk scoring, multi-factor decisions, conditional workflows, rating calculations
- **Reviewer Focus:** Risk assessment criteria, rating methodology, decision rules, decline reasons

**Sample 15: Insurance Subrogation**
- **Domain:** Insurance Subrogation (ACORD-aligned)
- **Key Features:** Subrogation identification, third-party claim, recovery process, settlement tracking
- **Fields:** Claim ID, at-fault party, liability assessment, recovery amount, legal status, settlement date
- **States:** Identified → Investigation → Demand Sent → Negotiation → Settlement/ litigation → Recovered/Closed
- **Research Reference:** Subrogation best practices, recovery workflows
- **Demonstrates:** Complex multi-party workflows, temporal tracking, conditional legal processes, financial tracking
- **Reviewer Focus:** Subrogation criteria, liability assessment, recovery process, settlement tracking

---

## Phase 2: Manufacturing & Quality Workflows (12 samples)

### Quality Control & Inspection (4 samples)

**Sample 9: Manufacturing Quality Inspection**
- **Domain:** Manufacturing Quality Control
- **Key Features:** Inspection workflows, defect tracking, corrective actions, statistical process control
- **Fields:** Product ID, batch number, inspection criteria, measurements, defect codes, corrective actions
- **States:** Scheduled → In Progress → Pass/Fail → Corrective Action → Closed
- **Research Reference:** ISO 9001 quality management, statistical process control patterns
- **Demonstrates:** Numeric constraints, measurement tolerances, workflow with data validation, corrective loops
- **Reviewer Focus:** Inspection criteria, tolerance ranges, corrective action requirements

**Sample 10: Incoming Material Inspection**
- **Domain:** Supply Chain Quality
- **Key Features:** Raw material inspection, supplier certification, quarantine, release decisions
- **Fields:** Material ID, supplier, purchase order, inspection results, quarantine status, release decision
- **States:** Received → Quarantined → Inspecting → Approved/Rejected → Released
- **Research Reference:** Incoming quality control, supplier material certification
- **Demonstrates:** Conditional workflows, quarantine logic, approval hierarchies
- **Reviewer Focus:** Inspection criteria, quarantine rules, release authority

**Sample 11: Final Product Inspection**
- **Domain:** Manufacturing Quality Assurance
- **Key Features:** Final quality check, packaging verification, certification, shipment release
- **Fields:** Product ID, batch number, inspection checklist, test results, certifications, shipment status
- **States:** Production Complete → Pending Inspection → Inspecting → Certified → Shipped/Quarantined
- **Research Reference:** Final quality assurance, product certification standards
- **Demonstrates:** Checklists, multi-criteria validation, certification workflows
- **Reviewer Focus:** Inspection criteria, certification requirements, release conditions

**Sample 12: Statistical Process Control**
- **Domain:** Manufacturing SPC
- **Key Features:** Control charts, process capability, trend analysis, out-of-control alerts
- **Fields:** Process ID, measurement values, control limits, sample size, trend indicators, alert status
- **States:** Monitoring → In Control → Warning → Out of Control → Corrective Action
- **Research Reference:** Statistical process control methodologies, control chart interpretation
- **Demonstrates:** Numeric calculations, threshold-based alerts, temporal trend analysis
- **Reviewer Focus:** Control limits, trend detection, alert thresholds

### Production & Work Orders (3 samples)

**Sample 13: Production Work Order**
- **Domain:** Manufacturing Production
- **Key Features:** Work order creation, material allocation, production tracking, completion verification
- **Fields:** Work order ID, product, quantity, materials, start date, completion date, status
- **States:** Created → Approved → In Progress → Quality Check → Completed
- **Research Reference:** Manufacturing execution systems, work order management
- **Demonstrates:** Resource allocation, temporal tracking, conditional routing based on quality
- **Reviewer Focus:** Material requirements, production scheduling, quality gate integration

**Sample 14: Production Schedule Management**
- **Domain:** Production Planning
- **Key Features:** Schedule creation, capacity planning, priority management, schedule changes
- **Fields:** Schedule ID, production line, start date, end date, priority, status, change history
- **States:** Draft → Approved → In Progress → Completed/Cancelled → Rescheduled
- **Research Reference:** Production planning and scheduling, capacity management
- **Demonstrates:** Temporal constraints, priority logic, conditional rescheduling
- **Reviewer Focus:** Capacity constraints, priority rules, rescheduling triggers

**Sample 15: Bill of Materials Management**
- **Domain:** Manufacturing Engineering
- **Key Features:** BOM structure, component tracking, revision control, impact analysis
- **Fields:** BOM ID, product, components, quantities, revisions, effective dates, status
- **States:** Draft → Approved → Active → Obsolete → Replaced
- **Research Reference:** Bill of materials management, product lifecycle management
- **Demonstrates:** Hierarchical data, revision control, effective dating, impact tracking
- **Reviewer Focus:** BOM structure, revision rules, obsolescence handling

### Supplier & Vendor Management (2 samples)

**Sample 16: Supplier Quality Management**
- **Domain:** Supply Chain Quality
- **Key Features:** Supplier evaluation, quality metrics, corrective actions, certification tracking
- **Fields:** Supplier ID, quality score, defect rates, certifications, corrective actions, audit dates
- **States:** Registered → Under Review → Approved → Monitored → Suspended
- **Research Reference:** Supplier quality management, vendor scorecarding
- **Demonstrates:** Scoring systems, temporal certifications, conditional status based on performance
- **Reviewer Focus:** Quality metrics, scoring methodology, suspension criteria

**Sample 17: Supplier Corrective Action Request (SCAR)**
- **Domain:** Supplier Quality
- **Key Features:** Defect reporting, root cause investigation, corrective action, effectiveness verification
- **Fields:** SCAR ID, supplier, defect description, quantity affected, root cause, corrective action, verification status
- **States:** Open → Investigating → Corrective Action Proposed → Implemented → Verified → Closed
- **Research Reference:** Supplier corrective action processes, 8D problem-solving
- **Demonstrates:** Complex workflows, temporal tracking, verification loops, multi-party communication
- **Reviewer Focus:** Root cause categories, corrective action effectiveness, verification criteria

### Equipment & Maintenance (3 samples)

**Sample 18: Equipment Maintenance Schedule**
- **Domain:** Equipment Management
- **Key Features:** Preventive maintenance scheduling, work order generation, completion tracking, overdue escalation
- **Fields:** Equipment ID, maintenance type, schedule frequency, last service, next due, status
- **States:** Scheduled → In Progress → Completed → Overdue/Cancelled
- **Research Reference:** Preventive maintenance best practices, equipment management
- **Demonstrates:** Temporal scheduling, recurring workflows, conditional escalation
- **Reviewer Focus:** Maintenance intervals, escalation thresholds, critical equipment handling

**Sample 19: Calibration Management**
- **Domain:** Equipment Calibration
- **Key Features:** Calibration scheduling, tolerance verification, certificate tracking, out-of-tolerance actions
- **Fields:** Equipment ID, calibration due date, tolerance limits, measured value, certificate, status
- **States:** Due → Scheduled → In Progress → Passed/Failed → Recalibrated/Quarantined
- **Research Reference:** Calibration management, metrology standards
- **Demonstrates:** Temporal tracking, numeric validation, conditional workflows based on tolerance
- **Reviewer Focus:** Tolerance specifications, calibration intervals, quarantine procedures

**Sample 20: Equipment Downtime Tracking**
- **Domain:** Equipment Management
- **Key Features:** Downtime logging, reason codes, availability calculation, maintenance impact
- **Fields:** Equipment ID, downtime start, downtime end, reason code, impact duration, resolution
- **States:** Running → Down → Diagnosing → Repairing → Restored
- **Research Reference:** Equipment effectiveness (OEE), downtime analysis
- **Demonstrates:** Temporal calculations, categorization, impact analysis
- **Reviewer Focus:** Downtime categories, recovery procedures, impact calculations

---

## Phase 3: SaaS & Subscription Workflows (8 samples)

### Subscription & Billing Management (4 samples)

**Sample 21: SaaS Subscription Billing**
- **Domain:** SaaS Subscription Management
- **Key Features:** Billing cycles, usage metering, proration, upgrade/downgrade workflows, invoice generation
- **Fields:** Subscription ID, plan type, usage metrics, billing cycle, amounts, proration adjustments, invoice status
- **States:** Active → Suspended → Upgrading/Downgrading → Cancelled → Reactivated
- **Research Reference:** `type-system-domain-survey.md` (SaaS billing fields), subscription management patterns
- **Demonstrates:** Temporal billing logic, usage-based calculations, subscription lifecycle, proration rules
- **Reviewer Focus:** Billing cycle handling, proration calculations, upgrade/downgrade rules

**Sample 22: SaaS Trial to Paid Conversion**
- **Domain:** SaaS Sales Funnel
- **Key Features:** Trial enrollment, feature limitations, conversion prompts, upgrade workflows, trial extensions
- **Fields:** Trial ID, user info, trial start/end dates, feature access, conversion status, extension count
- **States:** Trial Started → Active Trial → Trial Expiring → Conversion Attempted → Converted/Expired
- **Research Reference:** SaaS conversion funnel patterns, trial management
- **Demonstrates:** Temporal constraints, conditional feature access, conversion workflows, extension logic
- **Reviewer Focus:** Trial duration, feature gating, conversion triggers, extension policies

**Sample 23: SaaS Usage Metering & Billing**
- **Domain:** SaaS Usage-Based Billing
- **Key Features:** Usage tracking, meter aggregation, billing calculations, overage handling, threshold alerts
- **Fields:** Customer ID, meter type, usage quantity, billing period, rates, overage charges, alert thresholds
- **States:** Tracking → Aggregated → Billed → Paid/Overdue → Disputed
- **Research Reference:** Usage-based billing models, metering patterns
- **Demonstrates:** Aggregation functions (sum), temporal calculations, conditional billing, threshold alerts
- **Reviewer Focus:** Meter definitions, aggregation windows, overage thresholds, alert logic

**Sample 24: SaaS Plan Upgrade/Downgrade**
- **Domain:** SaaS Plan Management
- **Key Features:** Plan changes, effective dating, proration, feature access changes, usage limit adjustments
- **Fields:** Subscription ID, current plan, new plan, effective date, proration amount, status, usage limits
- **States:** Requested → Validated → Processing → Active/Rejected → Effective Date Pending
- **Research Reference:** Plan change workflows, proration methodologies
- **Demonstrates:** Effective dating, proration calculations, conditional transitions, feature access control
- **Reviewer Focus:** Proration logic, effective date handling, feature access changes, usage limit adjustments

### Customer Success & Retention (2 samples)

**Sample 25: SaaS Customer Success & Health Scoring**
- **Domain:** SaaS Customer Success
- **Key Features:** Customer health scoring, engagement tracking, risk prediction, proactive outreach, success planning
- **Fields:** Customer ID, health score, usage metrics, engagement score, risk factors, last contact date, success plan, outreach history
- **States:** Onboarded → Active → At Risk → Intervention → Recovered/Churned
- **Research Reference:** Customer success methodologies, health scoring frameworks, engagement analytics
- **Demonstrates:** Multi-factor scoring calculations, risk prediction logic, conditional outreach workflows, temporal engagement tracking
- **Reviewer Focus:** Health score methodology, risk factor weighting, intervention triggers, success metrics

**Sample 26: SaaS Customer Onboarding**
- **Domain:** SaaS Customer Success
- **Key Features:** Onboarding workflow, milestone tracking, training completion, success criteria, time-to-value
- **Fields:** Customer ID, onboarding plan, milestones, completion dates, training status, success criteria met
- **States:** Onboarding Started → Setup Complete → Training In Progress → Training Complete → Go-Live → Success
- **Research Reference:** SaaS onboarding best practices, customer success methodologies
- **Demonstrates:** Milestone tracking, temporal progress, conditional workflows, success metrics
- **Reviewer Focus:** Milestone definitions, success criteria, timeline expectations, escalation triggers

### Account & User Management (2 samples)

**Sample 27: SaaS User Provisioning**
- **Domain:** SaaS User Management
- **Key Features:** User account creation, role assignment, license allocation, deprovisioning, access audit
- **Fields:** User ID, email, role, license type, provision date, status, last login, deprovision reason
- **States:** Invited → Account Created → Active → Suspended → Deprovisioned
- **Research Reference:** Identity and access management, SaaS user lifecycle
- **Demonstrates:** Role-based access, temporal tracking, conditional workflows, audit trails
- **Reviewer Focus:** Role definitions, provisioning rules, deprovisioning criteria, audit requirements

**Sample 28: SaaS License Management**
- **Domain:** SaaS License Administration
- **Key Features:** License allocation, utilization tracking, reassignment, expiration management, compliance
- **Fields:** License ID, user assignment, license type, allocation date, expiration date, status, utilization metrics
- **States:** Available → Assigned → Expired → Revoked → Reallocated
- **Research Reference:** SaaS license management best practices, compliance requirements
- **Demonstrates:** Temporal tracking, allocation logic, conditional reassignment, compliance validation
- **Reviewer Focus:** License types, allocation rules, expiration handling, compliance requirements

---

## Reviewer Responsibilities (Steinbrenner)

### Research Grounding
- Access `research/` folder for industry domain patterns
- Cross-reference FHIR specifications for healthcare samples
- Verify ACORD standards for insurance samples
- Validate manufacturing quality patterns against ISO standards
- Review SaaS billing patterns against industry practices

### Review Checklist per Sample
1. **Field Completeness:** Are all necessary fields from research represented?
2. **State Coverage:** Do states reflect real-world workflow stages?
3. **Temporal Accuracy:** Are timeframes and deadlines realistic?
4. **Business Logic:** Do rules and conditions match industry practices?
5. **Edge Cases:** Are important edge cases and error conditions covered?
6. **Complexity:** Is the sample complex enough to demonstrate value without being unwieldy?

### Feedback Format
For each sample, Steinbrenner provides:
- ✅ **Approved** - No changes needed
- ⚠️ **Approved with Minor Feedback** - Minor adjustments recommended
- ❌ **Needs Revision** - Significant changes required

Feedback includes specific references to research documents and industry standards.

**What Precept does that no other tool does:**
1. **Binds state machines + data rules in one artifact** — not separate validators, flows, and triggers
2. **Structural prevention** — invalid configurations are impossible, not just flagged
3. **Non-mutating Inspect** — "what if" analysis across all possible transitions
4. **Deterministic, inspectable engine** — same inputs always produce same outputs

### Industry Standard Alignment

**Research-backed domains for new samples:**

| Industry | Standard | Research Reference | Precept Advantage |
|----------|----------|-------------------|-------------------|
| **Healthcare** | FHIR (Fast Healthcare Interoperability Resources) | `entity-governance-landscape.md` §5.1 | 150+ FHIR resources define workflows; Precept enforces them at the entity boundary |
| **Insurance** | ACORD (Association for Cooperative Operations Research and Development) | `entity-governance-landscape.md` §1.3, Guidewire analog | ACORD standards define 200+ entity types; Precept provides library-weight enforcement |
| **Finance** | ISO 20022 (Financial messaging) | Research notes on payment workflows | ISO message types define transaction states; Precept enforces lifecycle integrity |
| **Manufacturing** | SCOR (Supply Chain Operations Reference) | Type system domain survey | Production quality checkpoints, material requirements — all lifecycle-gated |
| **Compliance** | GDPR, PCI-DSS, SOC2, FDA 21 CFR Part 11 | Security survey, compliance patterns | Regulatory workflows require audit trails, approval chains, immutable records |

### Sample Tiering Strategy

**Tier 1 (Core Value):** 15 samples — Industry-grade workflows demonstrating Precept's unique binding of state + data  
**Tier 2 (Strong):** 12 samples — Enterprise workflows with moderate complexity  
**Tier 3 (Supporting):** 8 samples — Specialized domains and edge cases  
**Tier 4 (De-emphasized):** 3-4 samples — Stateless data entities only (minimal representation)

**Total:** ~38 new samples (bringing corpus to ~68 total)

### Feature Coverage Re-prioritized

**Workflow governance features (highest priority):**
- Multi-stage approval chains with amount-based routing
- Conditional editability based on workflow state
- Audit trails with immutable logs (appendBy pattern)
- SLA tracking with temporal deadlines
- Compliance-driven constraints (field-level editability)
- Parallel workflow paths with synchronization

**Domain-specific type support:**
- `choice` types for enumerated business values (41 fields across 10 domains)
- `date` types for calendar dates (30 fields across 10 domains)
- `decimal` types for financial precision (16 fields across 9 domains)
- `integer` types for discrete counts (11 fields across 7 domains)
- `duration`/`period` for temporal arithmetic (19 fields across samples)

**Collection types (secondary priority):**
- Queue operations for document/workflow queues
- Stack operations for undo/redo or step tracking
- Lookup collections for rate tables, configuration mappings
- Log-by (appendBy) for ordered audit trails
- List with index for document sections

**Note:** Technical features are now framed as **enablers of workflow governance**, not the primary demonstration goal.

---

## Proposed Samples: Research-Grounded, Industry-Aligned

### Phase 1: Industry-Grade Workflow Samples (Tier 1 — Core Value)

These 15 samples demonstrate Precept's unique value: binding state machines + data rules to make invalid configurations structurally impossible. Each is grounded in specific research findings and industry standards.

#### 1.1 Medical Prior Authorization (FHIR-Aligned)
**Filename:** `medical-prior-authorization.precept`  
**Research Reference:** `type-system-domain-survey.md` §10-domain survey (healthcare), `entity-governance-landscape.md` §5.1 (FHIR)  
**FHIR Resources Referenced:** Coverage, ExplanationOfBenefit, ServiceRequest, CarePlan  
**Features:**
- Multi-stage workflow: Submitted → Clinical Review → Medical Review → Adjudication → Authorized/Denied
- `choice` types: PriorAuthStatus, ClinicalIndication, MedicalNecessityCriteria
- `decimal` types: AuthorizedAmount, PatientResponsibility, ProviderCharge
- `date` types: ServiceDate, AuthorizationEffectiveDate, AuthorizationExpiration
- `integer` types: AuthorizedUnits, RemainingVisits
- Conditional editability: ClinicalNotes only editable during Clinical Review state
- Computed fields: `TotalAuthorized = AuthorizedAmount * AuthorizedUnits`
- SLA tracking: `DecisionDeadline = SubmissionDate + days(14)` (CMS requirement)
**Why This Demonstrates Precept's Value:**
- FHIR defines 150+ resources but doesn't enforce workflow integrity — Precept does
- Invalid state transitions (e.g., skipping Medical Review) are structurally impossible
- Temporal deadlines enforce regulatory compliance (CMS 14-day rule)

#### 1.2 Insurance Underwriting (ACORD-Aligned)
**Filename:** `insurance-underwriting.precept`  
**Research Reference:** `entity-governance-landscape.md` §1.3 (Guidewire), `type-system-domain-survey.md` (insurance underwriting domain)  
**ACORD Standards Referenced:** LOCA (Loss Cost), CLCA (Commercial Liability), PLCA (Personal Lines)  
**Features:**
- Multi-stage workflow: Application → Risk Assessment → Rate Quote → Underwriting Decision → Bound/Declined
- `choice` types: RiskClass, UnderwritingDecision, CoverageType, PolicyForm
- `decimal` types: PremiumAmount, LossRatio, ExposureBase, RateFactor
- `integer` types: CoverageLimits, DeductibleAmount, YearsOfExperience
- `date` types: EffectiveDate, ExpirationDate, QuoteExpiration
- Complex guards: `RiskScore < 700 AND LossRatio < 0.65 AND CoverageLimits >= 100000`
- Parallel review paths: Auto underwriting vs. manual underwriting based on risk score
- `lookup` collection: RateTable mapping RiskClass → RateFactor
**Why This Demonstrates Precept's Value:**
- ACORD defines entity vocabularies but not workflow enforcement
- Underwriting rules are complex and state-conditional (different rules at different stages)
- Invalid configurations (e.g., binding before approval) are structurally impossible

#### 1.3 Manufacturing Quality Control (SCOR-Aligned)
**Filename:** `manufacturing-quality-control.precept`  
**Research Reference:** `type-system-domain-survey.md` (manufacturing quality control domain), `sample-temporal-pattern-catalog.md` (MTBF patterns)  
**SCOR Processes Referenced:** Plan, Source, Make, Deliver, Return (quality checkpoints at each stage)  
**Features:**
- Multi-stage workflow: Raw Material Received → In-Process Inspection → Final Inspection → Released/Rejected
- `choice` types: QualityStatus, DefectType, InspectionMethod, RejectionReason
- `decimal` types: MeasurementValue, Tolerance, DefectRate, YieldPercentage
- `integer` types: SampleSize, DefectCount, PassCount, RejectionCount
- `duration` types: InspectionDuration, HoldDuration, ReworkDuration
- `period` types: InspectionFrequency, CalibrationInterval
- Aggregate functions: `DefectRate = DefectCount / SampleSize`, `YieldPercentage = PassCount / TotalCount * 100`
- SLA tracking: `HoldExpiration = InspectionDate + hours(48)` (maximum hold time)
- `log` collection: `InspectionHistory` with `appendBy InspectionTimestamp`
**Why This Demonstrates Precept's Value:**
- Quality checkpoints must occur in specific sequence (cannot skip stages)
- Measurement tolerances are state-conditional (different tolerances at different stages)
- Temporal SLAs enforce quality hold limits

#### 1.4 Grant Application Review
**Filename:** `grant-application-review.precept`  
**Research Reference:** `entity-governance-landscape.md` (government workflows), `type-system-domain-survey.md` (regulatory compliance domain)  
**Features:**
- Multi-stage workflow: Submitted → Administrative Review → Technical Review → Panel Review → Award/Declined
- `choice` types: ReviewStage, PanelDecision, EligibilityStatus, FundingCategory
- `decimal` types: RequestedAmount, ApprovedAmount, MatchingFundRequirement
- `integer` types: ReviewerScore, PanelRanking, PriorityRank
- `date` types: SubmissionDeadline, DecisionDate, AwardEffectiveDate, ProjectStartDate
- `period` types: ProjectDuration, ReportingInterval
- Multi-stage approval: Requires minimum 3 reviewer scores, panel consensus threshold
- `lookup` collection: FundingCategoryRates mapping category → max funding percentage
- Conditional editability: Budget details locked after Administrative Review
**Why This Demonstrates Precept's Value:**
- Multi-stage review with mandatory minimums (cannot skip stages or reviewers)
- Temporal deadlines enforce grant cycle schedules
- Funding rules are state-conditional (different rules at different stages)

#### 1.5 Construction Permit Application
**Filename:** `construction-permit-application.precept`  
**Research Reference:** `entity-governance-landscape.md` (government workflows), regulatory compliance patterns  
**Features:**
- Multi-stage workflow: Submitted → Document Review → Plan Check → Permit Issued / Returned for Corrections / Denied
- `choice` types: PermitType, ReviewStatus, CorrectionType, InspectionType
- `decimal` types: ConstructionCost, PermitFee, ImpactFee
- `integer` types: NumberOfStories, NumberOfUnits, SquareFootage
- `date` types: ApplicationDate, PermitIssueDate, ExpirationDate, InspectionDueDate
- `period` types: PermitValidityPeriod, InspectionInterval
- Document requirements: `RequiredDocuments = set("Blueprints", "EngineeringCalculations", "ZoningApproval")` based on PermitType
- Fee calculation: `PermitFee = ConstructionCost * rate(PermitType)`
- `queue` collection: `InspectionQueue` with `enqueueBy Priority` (safety-critical inspections first)
**Why This Demonstrates Precept's Value:**
- Document requirements vary by permit type (enforced at compile time)
- Fee calculations are type-safe (decimal precision)
- Inspection queue ordering enforces safety priorities

#### 1.6 SaaS Billing Cycle (Usage Metering)
**Filename:** `saas-billing-cycle.precept`  
**Research Reference:** `type-system-domain-survey.md` (SaaS billing domain), `sample-temporal-pattern-catalog.md` (subscription patterns)  
**Features:**
- Lifecycle states: Active → Suspended → Cancelled → Reactivated
- `choice` types: BillingCycle, PaymentStatus, SubscriptionTier, ProrationMethod
- `decimal` types: MonthlyPrice, UsageCharge, ProratedAmount, CreditBalance
- `integer` types: UsageQuantity, SeatCount, OverageUnits
- `date` types: BillingCycleStartDate, BillingCycleEndDate, NextBillingDate, ProrationDate
- `period` types: BillingPeriod, TrialPeriod, GracPeriod
- `duration` types: UsageWindow (e.g., 24-hour API call window)
- Computed fields: `CurrentUsageCharge = UsageQuantity * UnitPrice + OverageCharge`
- `appendBy` log: `BillingHistory` with `appendBy BillingTimestamp`
- Temporal logic: `NextBillingDate = BillingCycleEndDate + days(1)` (automatic renewal)
**Why This Demonstrates Precept's Value:**
- Billing cycles require precise temporal arithmetic (periods, not durations)
- Proration rules are state-conditional (different rules during trial vs. active)
- Usage metering requires accumulation patterns (log-by)

#### 1.7 Clinical Trial Participant Enrollment
**Filename:** `clinical-trial-participant.precept`  
**Research Reference:** `type-system-domain-survey.md` (clinical trials domain), FDA 21 CFR Part 11 compliance patterns  
**Features:**
- Multi-stage workflow: Screened → Eligible → Enrolled → Active → Completed / Withdrawn / Disqualified
- `choice` types: EnrollmentStatus, EligibilityCriterion, AdverseEventSeverity, VisitType
- `decimal` types: DosageAmount, LabValue, VitalSignMeasurement
- `integer` types: VisitNumber, DayOfStudy, AdverseEventCount
- `date` types: ScreeningDate, EnrollmentDate, LastVisitDate, StudyCompletionDate
- `duration` types: FollowUpPeriod, WashoutPeriod, ObservationWindow
- `period` types: VisitSchedule (e.g., weekly, monthly), StudyDuration
- Complex guards: `AllEligibilityCriteriaMet AND NoExclusionCriteriaPresent AND InformedConsentSigned`
- `log` collection: `AdverseEvents` with `appendBy EventOnsetDate`
- Temporal SLAs: `FollowUpWindow = VisitDate + days(7)` (mandatory follow-up window)
**Why This Demonstrates Precept's Value:**
- FDA 21 CFR Part 11 requires audit trails (appendBy log)
- Eligibility criteria are complex and state-conditional
- Temporal windows enforce clinical protocol requirements

#### 1.8 Employee Onboarding (Multi-Department)
**Filename:** `employee-onboarding.precept`  
**Research Reference:** `type-system-domain-survey.md` (employee onboarding domain), `sample-temporal-pattern-catalog.md` (parallel workflow patterns)  
**Features:**
- Multi-path workflow: Offer Accepted → IT Setup → HR Onboarding → Department Orientation → Benefits Enrollment → Completed
- `choice` types: OnboardingStage, Department, EquipmentType, TrainingRequirement
- `decimal` types: Salary, SigningBonus, RelocationAllowance
- `integer` types: SeatNumber, BadgeNumber, TrainingModulesCompleted
- `date` types: OfferDate, StartDate, EquipmentDeliveryDate, TrainingCompletionDate
- `period` types: ProbationPeriod, TrainingDeadline
- Parallel paths: IT, HR, Department tasks run in parallel, must all complete before "Active" state
- `queue` collection: `PendingTasks` with `enqueueBy Priority` (critical tasks first)
- Synchronization: `AllTasksComplete = Count(PendingTasks) == 0`
**Why This Demonstrates Precept's Value:**
- Parallel workflow paths with synchronization points
- Task priorities enforce critical path ordering
- State-conditional editability (salary locked after HR onboarding)

#### 1.9 Product Recall Management
**Filename:** `product-recall-management.precept`  
**Research Reference:** `entity-governance-landscape.md` (manufacturing), regulatory compliance patterns  
**Features:**
- Multi-stage workflow: Issue Identified → Investigation → Recall Decision → Notification → Remediation → Closed
- `choice` types: RecallClass, RootCause, RemediationType, NotificationMethod
- `decimal` types: AffectedUnits, CostOfRecall, RefundAmount
- `integer` types: UnitsReturned, UnitsReplaced, CustomerComplaints
- `date` types: IssueDetectedDate, RecallAnnouncementDate, RemediationDeadline
- `duration` types: InvestigationDuration, ResponseTime
- `period` types: NotificationWindow (e.g., 30 days from recall decision)
- Regulatory SLAs: `CPSCNotificationDeadline = IssueDetectedDate + days(30)` (mandatory)
- `log` collection: `CustomerNotitifications` with `appendBy NotificationDate`
- Aggregate functions: `TotalCost = CostOfRecall + RefundAmount + ReplacementCost`
**Why This Demonstrates Precept's Value:**
- Regulatory deadlines are mandatory (CPSC 30-day rule)
- Recall classes determine notification requirements (state-conditional)
- Audit trail required for regulatory compliance

#### 1.10 Mortgage Loan Servicing
**Filename:** `mortgage-loan-servicing.precept`  
**Research Reference:** `type-system-domain-survey.md` (loan servicing domain), ISO 20022 payment messaging  
**Features:**
- Multi-stage workflow: Origination → Funding → Active → Delinquent → Foreclosure → Charged-Off / Repaid
- `choice` types: LoanStatus, DelinquencyReason, PaymentApplicationMethod, ForeclosureStage
- `decimal` types: PrincipalBalance, InterestAccrued, EscrowBalance, PaymentAmount
- `integer` types: PaymentCount, DelinquentMonths, LateFeeCount
- `date` types: OriginationDate, FirstPaymentDate, LastPaymentDate, MaturityDate
- `period` types: LoanTerm (e.g., 30 years), GracePeriod
- `duration` types: PaymentProcessingTime, EscrowAnalysisInterval
- Complex calculations: `MonthlyPayment = Principal + Interest + Escrow + PMI`
- `appendBy` log: `PaymentHistory` with `appendBy PaymentDate`
- Temporal SLAs: `EscrowAnalysisDue = OriginationDate + months(12)` (annual requirement)
**Why This Demonstrates Precept's Value:**
- Financial precision required (decimal, not number)
- Payment application rules are state-conditional (delinquent vs. current)
- Regulatory requirements enforce escrow analysis timing

#### 1.11 Healthcare Patient Surgery Scheduling
**Filename:** `patient-surgery-scheduling.precept`  
**Research Reference:** FHIR resources (Appointment, Schedule, Slot, Procedure), `sample-temporal-pattern-catalog.md` (appointment patterns)  
**Features:**
- Multi-stage workflow: Requested → Pre-Op Clearance → Scheduled → Pre-Operative Hold → In Surgery → Recovering → Completed / Cancelled
- `choice` types: SurgeryType, PreOpStatus, AnesthesiaType, RecoveryStage
- `decimal` types: EstimatedDuration, ActualDuration, SurgeonFee
- `integer` types: OperatingRoomNumber, BedNumber, NursingStation
- `date` types: RequestDate, ScheduledDate, SurgeryDate, DischargeDate
- `time` types: ScheduledStartTime, ActualStartTime, ActualEndTime
- `duration` types: RecoveryTime, PreOpPreparationTime
- `period` types: PreOpWindow (e.g., 30 days before surgery), RecoveryWindow
- `lookup` collection: OperatingRoomSchedule mapping room → available time slots
- Temporal constraints: `SurgeryDate >= PreOpClearanceDate + days(7)` (minimum clearance window)
**Why This Demonstrates Precept's Value:**
- Time-of-day scheduling requires native `time` type
- Pre-op clearance windows enforce medical safety
- Operating room allocation requires lookup collections

#### 1.12 Insurance Claim Adjudication (Enhanced)
**Filename:** `insurance-claim-adjudication.precept`  
**Research Reference:** ACORD standards (Claim lifecycle), `entity-governance-landscape.md` (Guidewire analog)  
**ACORD Standards Referenced: CACA (Claim Activity), CLCA (Commercial Liability), PLCA (Personal Lines)**  
**Features:**
- Enhanced from existing `insurance-claim.precept`
- Multi-stage workflow: Reported → Assigned → Investigating → Reserve Set → Negotiating → Settlement Approved → Paid / Denied
- `choice` types: ClaimType, ReserveType, SettlementType, DenialReason
- `decimal` types: ClaimAmount, ReserveAmount, SettlementAmount, SubrogationRecovery
- `integer` types: AdjusterCount, WitnessCount, DocumentCount
- `date` types: LossDate, ReportDate, InvestigationStartDate, DecisionDate, SettlementDate
- `duration` types: InvestigationDuration, TimeToSettlement
- Complex fraud detection: Multiple rules across states (not just a single FraudFlag)
- `log` collection: `ClaimActivities` with `appendBy ActivityTimestamp`
- Regulatory SLAs: `DecisionDeadline = ReportDate + days(45)` (state-specific requirement)
**Why This Demonstrates Precept's Value:**
- ACORD defines claim vocabularies but not workflow enforcement
- Complex state-conditional rules (different rules at each stage)
- Audit trail required for regulatory compliance

#### 1.13 Financial Portfolio Rebalancing
**Filename:** `financial-portfolio-rebalancing.precept`  
**Research Reference:** `type-system-domain-survey.md` (investment management domain), ISO 20022 portfolio messaging  
**Features:**
- Lifecycle states: Established → Active → Rebalancing → Completed / Suspended
- `choice` types: AssetClass, RebalancingMethod, TransactionType, RiskProfile
- `decimal` types: AssetValue, TargetAllocation, CurrentAllocation, TransactionAmount
- `integer` types: ShareCount, TransactionCount, RebalancingTriggerCount
- `date` types: PortfolioDate, LastRebalanceDate, NextRebalanceDate
- `duration` types: HoldingPeriod, RebalancingWindow
- `period` types: RebalancingFrequency (e.g., quarterly, annually)
- Computed fields: `AllocationDeviation = abs(CurrentAllocation - TargetAllocation)`
- Aggregate functions: `TotalPortfolioValue = sum(AssetValues)`
- Rebalancing rules: `TriggerRebalancing if AllocationDeviation > threshold(AssetClass)`
**Why This Demonstrates Precept's Value:**
- Financial precision required (decimal types)
- Rebalancing rules are state-conditional
- Temporal triggers enforce rebalancing schedules

#### 1.14 SaaS Subscription Upgrade/Downgrade
**Filename:** `saas-subscription-modification.precept`  
**Research Reference:** `type-system-domain-survey.md` (SaaS billing domain), subscription lifecycle patterns  
**Features:**
- Lifecycle states: Active → Modification Requested → Proration Calculated → Confirmed → Completed
- `choice` types: SubscriptionTier, ModificationType, BillingCycle, ProrationMethod
- `decimal` types: CurrentPrice, NewPrice, ProratedCredit, ProratedCharge
- `integer` types: CurrentSeats, NewSeats, UsageLevel
- `date` types: ModificationDate, EffectiveDate, NextBillingDate
- `period` types: RemainingBillingPeriod, TrialPeriod
- Computed fields: `ProratedAmount = (NewPrice - CurrentPrice) * RemainingDays / TotalDays`
- Temporal logic: `EffectiveDate = ModificationDate + days(1)` (next cycle) or immediate
- `choice` validation: `ModificationType` determines allowed `SubscriptionTier` transitions
**Why This Demonstrates Precept's Value:**
- Proration calculations require precise decimal arithmetic
- Tier transition rules are state-conditional
- Temporal logic handles immediate vs. next-cycle changes

#### 1.15 Regulatory Compliance Audit Trail
**Filename:** `regulatory-compliance-audit.precept`  
**Research Reference:** GDPR, PCI-DSS, SOC2 compliance patterns, `entity-governance-landscape.md`  
**Features:**
- Lifecycle states: Audit Initiated → Evidence Collected → Findings Documented → Remediation Planned → Remediation Complete → Audit Closed
- `choice` types: ComplianceFramework, FindingSeverity, RemediationType, EvidenceType
- `decimal` types: RiskScore, RemediationCost
- `integer` types: FindingCount, EvidenceCount, RemediationStepsCompleted
- `date` types: AuditStartDate, EvidenceDeadline, RemediationDeadline, AuditCompletionDate
- `period` types: RemediationWindow, FollowUpInterval
- `appendBy` log: `AuditTrail` with `appendBy Timestamp` (immutable record)
- `log` collection: `Findings` with associated evidence and remediation steps
- Regulatory SLAs: `RemediationDeadline = FindingDate + days(30)` (PCI-DSS requirement)
- Conditional editability: Evidence locked after Audit Review state
**Why This Demonstrates Precept's Value:**
- Immutable audit trail required by all compliance frameworks
- Regulatory deadlines enforce remediation timelines
- State-conditional editability protects evidence integrity

---

### Phase 1 Implementation Drift (Recorded 2026-05-25)

Honest accounting of what actually shipped against the §1.1–§1.15 detailed specs above. Verified by direct grep of `samples/` on 2026-05-25.

**§1.x specs that shipped (4 of 15):**

| Spec | Shipped as | Notes |
|---|---|---|
| §1.1 Medical Prior Authorization | `samples/medical-prior-auth.precept` | Filename truncated from spec; FHIR alignment preserved |
| §1.2 Insurance Underwriting | `samples/insurance-underwriting.precept` | Shipped under exact spec filename |
| §1.3 Manufacturing Quality Control | `samples/manufacturing-quality-inspection.precept` | Renamed from spec; SCOR alignment preserved |
| §1.12 Insurance Claim Adjudication | `samples/insurance-claim-adjudication.precept` | Shipped under exact spec filename |

**§1.x specs that never shipped (11 of 15):**

- §1.4 Grant Application Review
- §1.5 Construction Permit Application
- §1.6 SaaS Billing Cycle (Usage Metering)
- §1.7 Clinical Trial Participant Enrollment
- §1.8 Employee Onboarding (Multi-Department)
- §1.9 Product Recall Management
- §1.10 Mortgage Loan Servicing
- §1.11 Healthcare Patient Surgery Scheduling
- §1.13 Financial Portfolio Rebalancing
- §1.14 SaaS Subscription Upgrade/Downgrade
- §1.15 Regulatory Compliance Audit Trail

Phase 1 in practice shipped a different set of 18 healthcare/insurance/manufacturing samples (see the Status table at the top of this document) — most of which aligned to the **Implementation Priority & Workflow** week-list further below, not to the §1.x detailed specs in this section. The §1.x specs above are now aspirational rather than authoritative.

**`appendBy` / `enqueueBy` pattern drift:**

§1.3, §1.5, §1.6, §1.7, §1.8, §1.9, §1.12, and §1.15 each promised `appendBy` or `enqueueBy` patterns in their **Features** lists. Of the 8, only §1.3 and §1.12 shipped at all, and **neither shipped sample uses `appendBy` or `enqueueBy`** (corpus-wide: 0 uses of either action). The canonical idiom for these patterns is queued in **Phase 5** below; existing samples can adopt them in a future quality sweep.

**Disposition for the 11 unshipped §1.x specs:**

- **Defer (default):** keep the spec text intact above; treat it as a backlog of candidate samples for future expansion waves. No action required now.
- **Drop:** explicitly retire any spec the team no longer wants. Mark with `Status: Dropped YYYY-MM-DD — reason` inline.
- **Re-prioritize into Phase 5:** if a spec's primary value is catalog showcase (e.g., §1.7 Clinical Trial / §1.9 Product Recall heavily exercise `appendBy`/`Irreversible`), it can be folded into a Phase 5 entry instead of shipping standalone.

This subsection is a snapshot, not a commitment. Update on each subsequent corpus audit.

---

### Phase 2: Enterprise Workflows (Tier 2 — Strong)

These 12 samples demonstrate enterprise-grade workflows with moderate complexity.

#### 2.1 Real Estate Transaction
**Filename:** `real-estate-transaction.precept`  
**Research Reference:** `entity-governance-landscape.md` (real estate domain), title/escrow workflows  
**Features:** Multi-party states (Offer → Accepted → Under Contract → Inspection → Appraisal → Financing → Closing), `choice` types for property classifications, `decimal` for purchase price and escrow amounts, `date` for closing dates and contingency deadlines.

#### 2.2 Legal Matter Management
**Filename:** `legal-matter-management.precept`  
**Research Reference:** Legal workflow patterns, discovery processes  
**Features:** Matter lifecycle (Intake → Conflict Check → Engaged → Active → Discovery → Resolution → Closed), `choice` types for matter types and billing codes, `duration` for billable hours tracking, `period` for statute of limitations.

#### 2.3 Supply Chain Order Fulfillment
**Filename:** `supply-chain-fulfillment.precept`  
**Research Reference:** SCOR model, inventory management patterns  
**Features:** Order lifecycle (Received → Validated → Picked → Packed → Shipped → Delivered → Completed), `choice` types for shipping methods, `decimal` for pricing and weights, `period` for delivery windows.

#### 2.4 Healthcare Prior Authorization (Denial Appeal)
**Filename:** `prior-authorization-appeal.precept`  
**Research Reference:** FHIR CoverageEligibilityRequest, healthcare denial management  
**Features:** Appeal workflow (Initial Denied → Appeal Filed → Clinical Review → Peer Review → Decision), `choice` types for appeal levels and denial reasons, `date` for appeal deadlines, `period` for appeal windows.

#### 2.5 Construction Change Order
**Filename:** `construction-change-order.precept`  
**Research Reference:** Construction management workflows, change order processes  
**Features:** Change order lifecycle (Requested → Estimated → Approved → Executed), `decimal` for cost impacts, `date` for schedule impacts, `choice` types for change categories.

#### 2.6 Grant Disbursement
**Filename:** `grant-disbursement.precept`  
**Research Reference:** Federal grant management, 2 CFR 200 compliance  
**Features:** Disbursement workflow (Approved → Obligated → Drawdown → Expended → Reported), `decimal` for fund amounts, `period` for reporting intervals, `date` for deadlines.

#### 2.7 Insurance Policy Endorsement
**Filename:** `insurance-policy-endorsement.precept`  
**Research Reference:** ACORD endorsement standards, policy modification workflows  
**Features:** Endorsement lifecycle (Requested → Underwritten → Approved → Issued), `choice` types for endorsement types, `decimal` for premium adjustments, `date` for effective dates.

#### 2.8 Manufacturing Bill of Materials (BOM) Approval
**Filename:** `bom-approval-workflow.precept`  
**Research Reference:** Manufacturing BOM processes, engineering change orders  
**Features:** BOM lifecycle (Draft → Engineering Review → Manufacturing Review → Approved), `choice` types for component classifications, `decimal` for quantities and costs.

#### 2.9 SaaS Customer Success Escalation
**Filename:** `customer-success-escalation.precept`  
**Research Reference:** SaaS customer success patterns, escalation workflows  
**Features:** Escalation lifecycle (Reported → Triage → Investigating → Resolving → Resolved), `choice` types for escalation levels and categories, `duration` for response times, `period` for SLA windows.

#### 2.10 Medical Device Incident Reporting
**Filename:** `medical-device-incident.precept`  
**Research Reference:** FDA Medical Device Reporting (MDR), 21 CFR 803  
**Features:** Incident lifecycle (Reported → Investigated → FDA Reported → Closed), `choice` types for incident severity and MDR codes, `duration` for reporting timelines (30-day FDA requirement), `date` for incident dates.

#### 2.11 Procurement Requisition
**Filename:** `procurement-requisition.precept`  
**Research Reference:** Procurement workflows, purchase order processes  
**Features:** Requisition lifecycle (Draft → Submitted → Approved → Ordered → Received), `decimal` for amounts, `choice` types for approval levels based on amount thresholds, `date` for delivery expectations.

#### 2.12 Clinical Trial Protocol Amendment
**Filename:** `clinical-trial-protocol-amendment.precept`  
**Research Reference:** FDA IND regulations, clinical trial protocol management  
**Features:** Amendment lifecycle (Proposed → IRB Review → Regulatory Review → Approved → Implemented), `choice` types for amendment types and impact levels, `date` for submission deadlines.

---

### Phase 3: Specialized Domains (Tier 3)

These 8 samples cover specialized domains and edge cases.

#### 3.1 Academic Course Registration
**Filename:** `academic-course-registration.precept`  
**Features:** Course registration workflow, `choice` types for course levels, `integer` for credit hours, `date` for registration periods, `period` for semester durations.

#### 3.2 Hotel Reservation Management
**Filename:** `hotel-reservation-management.precept`  
**Features:** Reservation lifecycle, `date` for check-in/out, `duration` for stay length, `period` for cancellation windows, `decimal` for rates.

#### 3.3 Equipment Lease Agreement
**Filename:** `equipment-lease-agreement.precept`  
**Features:** Lease lifecycle, `decimal` for lease payments, `period` for lease terms, `date` for commencement and expiration, `choice` types for lease classifications.

#### 3.4 Non-Profit Membership Renewal
**Filename:** `nonprofit-membership-renewal.precept`  
**Features:** Membership lifecycle, `period` for membership terms, `decimal` for dues, `date` for renewal deadlines, `choice` types for membership levels.

#### 3.5 Utility Service Connection
**Filename:** `utility-service-connection.precept`  
**Features:** Service connection workflow, `decimal` for deposits and charges, `date` for connection dates, `choice` types for service types.

#### 3.6 Event Venue Booking
**Filename:** `event-venue-booking.precept`  
**Features:** Booking lifecycle, `date` for event dates, `time` for start/end times, `decimal` for venue fees, `choice` types for venue spaces.

#### 3.7 Vehicle Registration Renewal
**Filename:** `vehicle-registration-renewal.precept`  
**Features:** Registration lifecycle, `period` for registration terms, `date` for expiration, `decimal` for fees, `choice` types for vehicle classifications.

#### 3.8 Library Inter-Library Loan
**Filename:** `library-interlibrary-loan.precept`  
**Features:** ILL workflow, `date` for borrowing/return dates, `period` for loan periods, `choice` types for material types.

---

### Phase 4: Stateless Supporting Samples (Tier 4 — De-emphasized)

These 3-4 samples provide supporting examples of data governance without lifecycle.

#### 4.1 Tax Rate Configuration
**Filename:** `tax-rate-configuration.precept`  
**Features:** Stateless tax rate governance, `decimal` for rates, `choice` types for tax jurisdictions, `lookup` collections for rate tables.

#### 4.2 Product Catalog
**Filename:** `product-catalog.precept`  
**Features:** Product data governance, `choice` types for product categories, `decimal` for prices, `dimensional` types for product specifications.

#### 4.3 Currency Exchange Rates
**Filename:** `currency-exchange-rates.precept`  
**Features:** Exchange rate governance, `decimal` for rates, `choice` types for currency codes (ISO 4217), `date` for rate effective dates.

#### 4.4 Unit of Measure Reference
**Filename:** `unit-of-measure-reference.precept`  
**Features:** UCUM-aligned unit definitions, `choice` types for unit categories, dimensional analysis rules.

---

### Phase 5: Catalog Coverage Showcase (Added 2026-05-25)

These 4 samples exist specifically to exercise catalog members that have zero idiomatic uses anywhere in the current corpus. Verified by direct grep of `samples/` on 2026-05-25. Bug-blocked patterns (BUG-006 interval narrowing, BUG-012 ordered-choice literal proof, etc.) are tracked separately in `docs/Working/bugs.md` and are out of scope here — Phase 5 covers only non-bug-blocked catalog gaps.

#### 5.1 Priority Incident Queue
**Filename:** `priority-incident-queue.precept`  
**Pair:** §5.2 (read as a unit — both demonstrate write-by-ordering collection idioms)  
**Catalog Members Targeted:** `QueueBy` type, `enqueueBy` action, `dequeueBy` action — all currently at zero corpus uses.  
**Domain:** SRE / service-desk incident response queue, prioritized by severity, dequeued FIFO within priority band.  
**Features:**
- `QueueBy Severity` collection of pending incidents
- `enqueueBy Severity` on the IncidentRaised event
- `dequeueBy` on the IncidentClaimed event
- State machine: Queued → Claimed → Investigating → Resolved
- `choice` types: Severity (SEV1..SEV4 ordered), IncidentStatus
- `duration` field for time-to-claim SLA
**Why This Demonstrates Precept's Value:**
- Priority queue ordering is structural, not enforced by application code
- Severity ranking is sealed at compile time (catalog-driven choice ordering)
- The canonical idiom for any priority-workflow domain (oncall, support tiers, dispatch)

#### 5.2 Regulatory Audit Log
**Filename:** `regulatory-audit-log.precept`  
**Pair:** §5.1  
**Catalog Members Targeted:** `LogBy` type, `appendBy` action — both currently at zero corpus uses.  
**Domain:** Immutable, append-only audit trail for a compliance-bound entity (HIPAA access log / SOX change log shape).  
**Features:**
- `LogBy Timestamp` collection of audit entries
- `appendBy Timestamp` on every state-changing event
- Each entry records actor, action, target, and timestamp — never modified after append
- Lifecycle state where the log is unbounded but per-entry fields are `Irreversible` (cross-references §5.4)
**Why This Demonstrates Precept's Value:**
- Append-only is structural, not policy — entries cannot be edited or removed even by privileged code
- Required by all compliance frameworks (GDPR, HIPAA, SOX, PCI-DSS) — Precept enforces it at the entity boundary
- The canonical idiom for any audit-trail or event-sourcing-lite domain

#### 5.3 Sensor Measurement Aggregation
**Filename:** `sensor-measurement-aggregation.precept`  
**Catalog Members Targeted:** Math functions on `Number` — `approximate`, `floor`, `ceil`, `clamp`, `sqrt`, possibly `pow` — all currently at zero corpus uses. (`Number` type itself is well-covered in 18 existing samples; this sample is specifically about the unused math surface.)  
**Domain:** Aggregated sensor reading (e.g., IoT temperature/pressure feed) with tolerance bands, rolling averages, and statistical thresholds.  
**Features:**
- `Number` fields for raw readings, computed averages, and tolerance bounds
- `approximate` modifier on the average — honest about the fact that aggregated readings are not exact
- `clamp` to bound a derived value within calibration limits
- `sqrt` and `pow` for variance/standard-deviation calculations
- `floor` / `ceil` for sample-window bucketing
- Lifecycle: Active → Out-of-Tolerance → Calibrating → Active
**Why This Demonstrates Precept's Value:**
- "Honesty about approximation" is a core principle (`docs/philosophy.md`) — `approximate` makes it visible in the type system
- Bounded numeric computation prevents silent overflow or out-of-range derived values
- Demonstrates the math surface in a domain where approximation is intrinsic, not accidental

#### 5.4 Regulatory Submission Package
**Filename:** `regulatory-submission-package.precept`  
**Catalog Members Targeted:** `Irreversible` modifier, `Mincount` modifier on collections, `Maxcount` modifier on collections — all currently at zero corpus uses. Also exercises `Warning` and `Error` constraint severities in modifier position (currently used only as identifier strings in choice values, never as constraint severities).  
**Domain:** Regulatory filing (FDA submission, SEC 10-K, IND application shape) where the package locks structurally once filed.  
**Features:**
- `set of Attachment` with `Mincount 1` (mandatory minimum) and `Maxcount 50` (per-filing cap)
- Per-attachment fields marked `Irreversible` after the SubmitFiling event — structurally locked, not policy-locked
- Two constraint severities demonstrated: `Warning` for soft-fail rules (e.g., attachment naming convention) and `Error` for hard-fail rules (e.g., signature missing)
- Lifecycle: Draft → Validating → Submitted → AcceptanceReceived/Rejected
**Why This Demonstrates Precept's Value:**
- "Prevention, not detection" — once filed, the package is structurally locked; no application-layer guard can be bypassed
- Demonstrates the difference between Warning and Error severities, which today exist only in the catalog without idiomatic showcase
- Mincount/Maxcount enforce regulatory bounds at compile time, not at runtime validation

#### Function coverage note

Of the ~14 unused functions identified in the 2026-05-25 corpus audit, §5.3 covers the math subset (`approximate`, `floor`, `ceil`, `clamp`, `sqrt`, possibly `pow`). The remaining temporal and string functions are not given a dedicated sample — they will be folded into existing samples opportunistically during future quality sweeps, since each function in isolation does not warrant its own showcase domain.

---

## Implementation Priority & Workflow

### Phase 1: Healthcare & Insurance Workflows (Weeks 1-3)
**Goal:** Establish industry-grade samples with 15 workflow-focused examples

**Week 1 - Healthcare Core (5 samples):**
1. medical-prior-authorization.precept (FHIR alignment)
2. prior-authorization-appeal.precept (FHIR Appeal)
3. patient-care-plan-coordination.precept (FHIR CarePlan)
4. prescription-refill-request.precept
5. lab-test-order-results.precept

**Week 2 - Healthcare Extended (5 samples):**
6. patient-referral-management.precept
7. utilization-review-case.precept
8. medical-claims-processing.precept (FHIR Claim)
9. patient-enrollment.precept
10. medical-device-tracking.precept

**Week 3 - Insurance Workflows (5 samples):**
11. insurance-claim-adjudication.precept (ACORD)
12. insurance-policy-endorsement.precept (ACORD)
13. insurance-renewal-processing.precept (ACORD)
14. insurance-underwriting.precept (ACORD)
15. insurance-subrogation.precept (ACORD)

### Phase 2: Manufacturing & Quality Workflows (Weeks 4-5)
**Goal:** Add 12 samples covering quality control, production, suppliers, and equipment

**Week 4 - Quality Control & Production (7 samples):**
16. manufacturing-quality-inspection.precept
17. incoming-material-inspection.precept
18. final-product-inspection.precept
19. statistical-process-control.precept
20. production-work-order.precept
21. production-schedule-management.precept
22. bill-of-materials-management.precept

**Week 5 - Suppliers & Equipment (5 samples):**
23. supplier-quality-management.precept
24. supplier-corrective-action-request.precept (SCAR)
25. equipment-maintenance-schedule.precept
26. calibration-management.precept
27. equipment-downtime-tracking.precept

### Phase 3: SaaS & Subscription Workflows (Week 6)
**Goal:** Add 8 samples covering billing, customer success, and user management

**Week 6 - SaaS Complete (8 samples):**
28. saas-subscription-billing.precept
29. saas-trial-to-paid-conversion.precept
30. saas-usage-metering-billing.precept
31. saas-plan-upgrade-downgrade.precept
32. saas-customer-retention.precept
33. saas-customer-onboarding.precept
34. saas-user-provisioning.precept
35. saas-license-management.precept
32. utility-service-connection.precept
33. event-venue-booking.precept
34. vehicle-registration-renewal.precept
35. library-interlibrary-loan.precept

### Phase 4: Stateless Supporting Samples (Week 9)
**Goal:** Add 4 Tier 4 samples for data governance coverage

36. tax-rate-configuration.precept
37. product-catalog.precept
38. currency-exchange-rates.precept
39. unit-of-measure-reference.precept

### Phase 5: Catalog Coverage Showcase (Added 2026-05-25)
**Goal:** Author 4 samples that exercise catalog members with zero idiomatic corpus uses; pair §5.1/§5.2 as a log/queue idiom unit.

40. priority-incident-queue.precept (paired with §5.2)
41. regulatory-audit-log.precept (paired with §5.1)
42. sensor-measurement-aggregation.precept
43. regulatory-submission-package.precept

---

## Quality Criteria for New Samples

Each new sample must:

1. ✅ Compile without errors or warnings
2. ✅ Demonstrate Precept's core value: binding state machines + data rules
3. ✅ Be grounded in research (reference specific research documents)
4. ✅ Align with industry standards where applicable (FHIR, ACORD, ISO 20022, etc.)
5. ✅ Include comprehensive comments explaining design decisions and research basis
6. ✅ Follow established naming conventions (kebab-case filenames)
7. ✅ Have meaningful field names, state names, and rule names
8. ✅ Include appropriate rules and ensures that reflect real business logic
9. ✅ Show proper use of typed constants for domain values
10. ✅ Demonstrate temporal patterns with native types (date, duration, period) where applicable
11. ✅ Use collection types (queue, stack, lookup, log, appendBy) to solve real workflow problems
11. ✅ Be self-contained (no external dependencies)

**New criteria specific to workflow governance:**
12. ✅ Demonstrate state-conditional rules (different rules at different lifecycle stages)
13. ✅ Show temporal SLAs or deadlines where applicable
14. ✅ Include audit trail patterns (appendBy logs) for compliance scenarios
15. ✅ Reference research documents in comments (e.g., "See: type-system-domain-survey.md §insurance-underwriting")

---

## External Research Notes & Industry Standards

### Industry Standards Referenced in This Plan

| Standard | Domain | Research Reference | Samples Using This Standard |
|----------|--------|-------------------|----------------------------|
| **FHIR (Fast Healthcare Interoperability Resources)** | Healthcare | `entity-governance-landscape.md` §5.1 | medical-prior-authorization, patient-surgery-scheduling, prior-authorization-appeal |
| **ACORD (Association for Cooperative Operations Research and Development)** | Insurance | `entity-governance-landscape.md` §1.3 (Guidewire) | insurance-underwriting, insurance-claim-adjudication, insurance-policy-endorsement |
| **ISO 20022** | Financial Services | Research notes on payment messaging | mortgage-loan-servicing, financial-portfolio-rebalancing |
| **SCOR (Supply Chain Operations Reference)** | Manufacturing/Supply Chain | `type-system-domain-survey.md` (manufacturing quality) | manufacturing-quality-control, supply-chain-fulfillment |
| **FDA 21 CFR Part 11** | Clinical Trials | FDA MDR patterns, clinical trial workflows | clinical-trial-participant, clinical-trial-protocol-amendment |
| **FDA 21 CFR 803** | Medical Device Reporting | Medical device incident patterns | medical-device-incident |
| **GDPR** | Data Privacy | Compliance patterns | regulatory-compliance-audit |
| **PCI-DSS** | Payment Card Industry | Compliance patterns | regulatory-compliance-audit |
| **SOC2** | Service Organization Control | Compliance patterns | regulatory-compliance-audit |
| **UCUM (Unified Code for Units of Measure)** | Units of Measure | `research/language/ucum-tier1-curation.md` | unit-of-measure-reference |
| **ISO 4217** | Currency Codes | Type system domain survey | currency-exchange-rates |
| **2 CFR 200** | Federal Grants | Grant management patterns | grant-disbursement |

### Best Practices for Workflow Governance Samples

1. **Research-grounded design** - Each sample must reference specific research documents that informed its design
2. **Industry standard alignment** - Where applicable, align with existing industry standards (FHIR, ACORD, ISO 20022)
3. **State-conditional rules** - Demonstrate how rules vary by lifecycle stage (core Precept value)
4. **Temporal SLAs** - Include regulatory or business deadlines where applicable
5. **Audit trail patterns** - Use `appendBy` logs for compliance-required audit histories
6. **Progressive complexity** - Start with core workflow, add complexity (parallel paths, conditional editability)
7. **Named patterns** - Reference known patterns (form-fill, lookup tables, priority queues)
8. **Comment density** - Explain the "why" (business rationale, research basis) not just the "what"
9. **Multi-file coherence** - Related samples should share patterns (e.g., all FHIR-aligned samples use consistent field naming)
10. **Test coverage** - Each sample should exercise specific language features and workflow patterns

---

## Quality Criteria for New Samples

Each new sample must:

1. ✅ Compile without errors or warnings
2. ✅ Demonstrate at least 2-3 previously unrepresented features
3. ✅ Be realistic and domain-grounded
4. ✅ Include comprehensive comments explaining design decisions
5. ✅ Follow established naming conventions
6. ✅ Have meaningful field names and state names
7. ✅ Include appropriate rules and ensures
8. ✅ Show proper use of typed constants
9. ✅ Demonstrate good guard conditions
10. ✅ Be self-contained (no external dependencies)

---

## Success Metrics

### Coverage Metrics
- **Industry domains**: 8 new domains represented (Healthcare/FHIR, Insurance/ACORD, Finance/ISO 20022, Manufacturing/SCOR, Regulatory Compliance, SaaS/Subscription, Legal, Real Estate)
- **Language features**: All core features demonstrated in workflow context (state machines, temporal types, collections, computed fields, guards, ensures)
- **Research grounding**: 100% of samples reference at least one research document

### Quality Metrics
- **Workflow governance demonstration**: Each Tier 1 sample clearly demonstrates Precept's unique value (binding state + data)
- **Industry standard alignment**: Samples referencing standards (FHIR, ACORD, ISO) accurately reflect those standards' entity models
- **Production-ready**: Samples compile without errors/warnings and include comprehensive comments

### Documentation Metrics
- **Research citations**: Each sample includes comment referencing the research that informed its design
- **Business rationale**: Comments explain "why" the workflow exists, not just "what" it does
- **Pattern documentation**: Samples demonstrate recognized patterns (audit trails, SLA tracking, parallel workflows)

### Testing Metrics
- **Test fixture ready**: Samples can serve as xUnit test fixtures with predictable behavior
- **Regression coverage**: Each sample exercises specific features for regression testing
- **Progression validation**: Samples show clear progression from simple to complex workflows

---

## Next Steps

### Immediate Actions (Week 0)
1. **Review this plan** with the Precept team (Frank, Shane, George, Steinbrenner)
2. **Validate research alignment** — Confirm that each Tier 1 sample's research references are accurate
3. **Assign ownership** — Assign Tier 1 samples to developers based on domain expertise
4. **Set up tracking** — Create GitHub issues for each sample with research references in the body

### Implementation Workflow (Weeks 1-9)
5. **Create samples iteratively** — One sample per day, validating compilation and behavior
6. **Research-first approach** — Before writing each sample, review the referenced research documents
7. **Comment as you go** — Include research citations and business rationale in comments
8. **Peer review** — Each sample reviewed by someone with domain expertise
9. **Test fixture validation** — Ensure each sample can serve as a test fixture with predictable behavior

### Post-Implementation
10. **Document usage patterns** — Capture any new patterns discovered during implementation
11. **Update language documentation** — Reflect any language features that needed expansion
12. **Create sample guide** — Develop a user guide that walks through samples by complexity and domain
13. **MCP tool alignment** — Ensure MCP tools can demonstrate sample workflows

---

*Plan revised: 2026-05-20*  
*Author: Frank (Lead/Architect & Language Designer)*  
*Research basis: 80+ research documents across 8 industry domains*  
*Status: Ready for team review and implementation*
