# Realistic Domain Benchmarks for Precept Samples

**Author:** J. Peterman  
**Studied:** 2026-04-07  
**Purpose:** Ground future sample design in real operating domains instead of generic workflow theater.

---

## 1. Local positioning check: what Precept says it is

Before looking outward, the product's own story is clear:

- Precept is a **domain integrity engine**: one executable contract that binds state, data, and business rules so invalid states are structurally impossible.
- The product is strongest where a **single business entity** moves through a governed lifecycle and every decision needs to stay inspectable.
- The language is intentionally closer to **authored business policy** than to general-purpose programming.
- The AI-first angle matters, but it is secondary to the core promise: **deterministic, inspectable domain control**.

That framing matters for sample choice. The best samples for Precept are not generic automations. They are governed case files: requests, claims, grants, disputes, incidents, and reviews.

Relevant local context: `README.md`, `design\brand\brand-decisions.md`, `design\brand\philosophy.md`, `samples\insurance-claim.precept`, `samples\loan-application.precept`, `samples\travel-reimbursement.precept`, `samples\building-access-badge-request.precept`.

---

## 2. What the external research says

Across insurance, healthcare, finance, IAM, compliance, case management, and incident response, the same workflow shape keeps appearing:

1. **Intake** — a request, report, case, or dispute is opened.
2. **Completeness / eligibility check** — is enough information present to proceed?
3. **Assignment or routing** — the work moves to the right reviewer, adjuster, approver, or team.
4. **Evidence collection loop** — missing documents, clarifications, or supporting artifacts bounce the case back and forth.
5. **Risk / policy review** — rules, thresholds, watchlists, budgets, segregation-of-duties, coverage, or severity are checked.
6. **Decision** — approve, deny, escalate, request more info, or partially approve.
7. **Fulfillment** — payout, reimbursement, access grant, shipment, repair, recovery, payment, or closure work happens after the decision.
8. **Audit / review / reopen** — many real domains include appeals, representment, recertification, post-incident review, or formal closure logging.

That shape is exactly where Precept feels native: one entity, explicit states, explicit events, readable guards, and durable rules.

---

## 3. Adjacent-system benchmark: where workflow tools point

Comparable and adjacent workflow systems consistently showcase the same kinds of work:

- **Temporal** explicitly uses examples like **order processing** and **customer onboarding**, and emphasizes long-running, resilient, deterministic workflows with event history ([Temporal Workflows](https://docs.temporal.io/workflows)).
- **AWS Step Functions** positions itself around **distributed applications**, **process automation**, **microservice orchestration**, **data pipelines**, and **long-running workflows requiring human interaction** ([AWS Step Functions overview](https://docs.aws.amazon.com/step-functions/latest/dg/welcome.html)).
- **Microsoft Entra Lifecycle Workflows** frames identity governance around **joiner / mover / leaver** lifecycles, automated tasks, audit logs, and timely access revocation ([What are lifecycle workflows?](https://learn.microsoft.com/en-us/entra/id-governance/what-are-lifecycle-workflows)).
- **Appian public-sector case management** describes government casework as workflows with **multiple steps, documents, regulatory processes, reviewers, and approvers** ([Appian public sector case management](https://appian.com/blog/acp/public-sector/case-management-process-public-sector)).

The lesson is useful: the market teaches workflow products through **serious, audit-heavy, document-bearing cases**. When samples become toy approval ladders, they fall beneath the category's own standard.

Just as important: Precept should resist drifting into examples that are really about **service orchestration** or **data-pipeline choreography**. That is adjacent territory, but not the product's clearest narrative. Precept's home ground is the contract around **an entity's lifecycle**, not a graph of infrastructure tasks.

---

## 4. Domain-by-domain benchmark

| Domain | Real-world shape from research | Why it fits Precept | What makes the sample credible | Useful sources |
|---|---|---|---|---|
| **Healthcare prior authorization** | Submission, clinical review, requests for additional information, approval/denial, appeal, strict response windows | Strong fit. It is a governed request with hard deadlines, document loops, and explicit denial reasons | Include missing-clinical-info loops, urgent vs routine timing, denial reasons, and appeal/review states. The credible detail is not medicine itself; it is the regulated approval choreography | [CMS final rule](https://www.cms.gov/priorities/burden-reduction/overview/interoperability/policies-regulations/cms-interoperability-prior-authorization-final-rule-cms-0057-f), [AMA resources](https://www.ama-assn.org/practice-management/prior-authorization/prior-authorization-practice-resources), [AHA summary](https://www.aha.org/news/blog/2024-02-15-prior-authorization-final-rule-will-improve-patient-access-alleviate-hospital-administrative-burdens) |
| **Insurance claim handling** | FNOL, triage, assignment, document collection, investigation, coverage review, fraud check, approval/denial, payout | Already a strong Precept domain. One claim, many decision points, evidence loops, possible edit windows | The sample should feel like a file under review: missing documents, assigned adjuster, fraud flag, partial approvals, payment readiness. Current sample coverage is good but still cleaner than real claims work | [Fluix claims process](https://fluix.io/workflows/insurance-claims-process), [Five Sigma on FNOL](https://fivesigmalabs.com/blog/optimizing-the-claims-process-starting-with-fnol/), [Terra on claims bottlenecks](https://terra.insure/blog/claims-workflow-bottlenecks-automation-tools/) |
| **Identity and access request / governance** | Request, manager approval, SoD/compliance check, provisioning, attestation, recertification, deprovisioning | Excellent fit. This is governed state plus policy plus audit trail, especially for role grants and privileged access | Make the sample about an access grant or privileged role, not a vague ticket. Include manager approval, SoD failure, training/attestation gate, periodic review or revocation | [Microsoft Entra lifecycle workflows](https://learn.microsoft.com/en-us/entra/id-governance/what-are-lifecycle-workflows), [Lifecycle workflow deployment](https://learn.microsoft.com/en-us/entra/id-governance/lifecycle-workflows-deployment) |
| **KYC / AML onboarding** | Customer identification, document verification, watchlist screening, risk scoring, EDD, escalation, suspicious activity reporting, closure | Excellent fit. This is one of the cleanest examples of domain integrity as policy enforcement | Use risk tiers, sanctioned-country checks, required documents, EDD escalation, and closure with retained justification. A realistic sample should feel like a compliance file, not a registration form | [SWIFT KYC process](https://www.swift.com/risk-and-compliance/know-your-customer-kyc/kyc-process), [Thomson Reuters KYC/AML onboarding](https://legal.thomsonreuters.com/blog/5-essential-steps-for-kyc-aml-onboarding-and-compliance/), [FirstAML workflow-first compliance](https://www.firstaml.com/resources/workflow-first-compliance-structuring-amlctf-for-real-world-delivery/) |
| **Payment dispute / chargeback** | Intake, inquiry / request for information, evidence submission, chargeback, representment, pre-arbitration, arbitration, refund/settlement, closure | Very strong fit. The workflow is explicit, adversarial, time-bound, and evidence-heavy | Credibility comes from network phases and deadlines. Include inquiry vs formal dispute, merchant evidence, escalation to arbitration, and closed-with-liability outcome | [Stripe disputes lifecycle](https://docs.stripe.com/disputes/how-disputes-work), [Adyen dispute flow](https://docs.adyen.com/risk-management/understanding-disputes/dispute-process-and-flow/), [Mastercard chargebacks guide](https://www.mastercard.us/content/dam/public/mastercardcom/na/global-site/documents/chargebacks-made-simple-guide.pdf) |
| **Public-sector or benefits case management** | Intake, eligibility, assignment, evidence collection, review, decision, appeal, closure | Strong fit. It emphasizes that Precept is not just for commerce; it can govern civic or regulated casework too | The sample should include required documents, eligibility rules, assignment, request-for-more-information loops, decision notice, and appeal. Avoid making it a generic support ticket | [Appian public sector case management](https://appian.com/blog/acp/public-sector/case-management-process-public-sector), [Creately case management guide](https://creately.com/guides/case-management-process/) |
| **Invoice approval / AP exception handling** | Invoice receipt, verification, 3-way match, discrepancy queue, approval routing, payment scheduling | Good fit, especially for finance operations and exceptions | A credible sample should center on mismatch resolution: PO mismatch, receipt missing, price variance, escalation threshold, payment hold. The exception path is the story | [Tipalti invoice approval workflow](https://tipalti.com/resources/learn/invoice-approval-workflow/), [Procurify purchase approval workflows](https://www.procurify.com/blog/purchase-approval-workflows/) |
| **Security incident case** | Detection, triage, severity classification, containment, investigation, evidence handling, recovery, lessons learned | Good fit for a supporting sample; less ideal for a first sample because time and orchestration pressures can overshadow the entity contract | Focus on a single incident record: severity, evidence bag, containment gate, recovery gate, mandatory postmortem before closure | [NIST Incident Response project](https://csrc.nist.gov/projects/incident-response) |
| **Returns / warranty / RMA** | Return request, authorization, receipt, inspection, repair/replacement/refund, closure | Still a good fit and close to current sample coverage | Credibility rises when inspection can fail, warranty eligibility can deny, and replacement/refund choices remain explicit. The sample should include inspection outcomes, not just a straight return | [Webretailer order fulfillment and returns](https://www.webretailer.com/shipping-fulfillment-returns/order-fulfillment-process/), [Atomix fulfillment guide](https://www.atomixlogistics.com/blog/order-fulfillment-process-3pl-ecommerce-shipping) |

---

## 5. Concrete facts worth carrying into sample design

### Healthcare prior authorization

The domain is regulated enough to avoid feeling invented. CMS's current final rule pushes toward standardized prior-authorization APIs and makes turnaround times part of the real workflow: **72 hours for urgent requests and 7 calendar days for standard requests** in the cited summaries around the rule. That gives future samples permission to model urgency classes, deadline-sensitive states, and explicit denial reasons without feeling contrived ([CMS final rule](https://www.cms.gov/priorities/burden-reduction/overview/interoperability/policies-regulations/cms-interoperability-prior-authorization-final-rule-cms-0057-f)).

### IAM / access governance

Microsoft's own lifecycle framing is **joiner / mover / leaver**. That is almost perfect sample language: one subject, access changes over time, and timeliness plus auditability matter as much as the action itself ([Microsoft Entra Lifecycle Workflows](https://learn.microsoft.com/en-us/entra/id-governance/what-are-lifecycle-workflows)).

### KYC / AML

SWIFT's explanation is useful because it is not abstract brand copy. It explicitly calls out collection of entity information, screening against sanctions and PEP/watch lists, risk rating, and enhanced due diligence above threshold. That yields credible sample ingredients immediately: risk band, watchlist hit, EDD escalation, and approval refusal based on unresolved risk ([SWIFT KYC process](https://www.swift.com/risk-and-compliance/know-your-customer-kyc/kyc-process)).

### Payment disputes

Stripe's dispute lifecycle includes **early fraud warnings**, **inquiries**, and the formal dispute process. That extra pre-dispute phase is exactly the sort of detail that separates a credible sample from a child's drawing of a workflow ([Stripe disputes lifecycle](https://docs.stripe.com/disputes/how-disputes-work)).

### Incident response

NIST's current framing is no longer just the old four-phase folk summary. It places incident response inside a broader model of **Detect, Respond, Recover**, with continuous improvement feeding back into the system. That makes incident samples stronger when they require a post-incident review or lessons-learned artifact before true closure ([NIST Incident Response project](https://csrc.nist.gov/projects/incident-response)).

### Finance approvals

The strongest AP/procurement sources do not stop at "approve invoice." They emphasize **routing by thresholds**, **budget/compliance checks**, **3-way match**, and especially **exception handling**. The mismatch queue is what makes the workflow real ([Procurify purchase approval workflows](https://www.procurify.com/blog/purchase-approval-workflows/), [Tipalti invoice approval workflow](https://tipalti.com/resources/learn/invoice-approval-workflow/)).

---

## 6. What current Precept samples already do well

The current sample set is already pointed in the right direction.

- `insurance-claim.precept` includes missing-document handling, adjuster assignment, approval/denial, and an editable fraud flag.
- `loan-application.precept` shows compound policy checks before approval.
- `travel-reimbursement.precept` uses a realistic finance ratio instead of empty toy arithmetic.
- `building-access-badge-request.precept` turns an approval into a governed access grant with a concrete business object.
- `warranty-repair-request.precept` uses a work log and explicit repair completion before return.
- `maintenance-work-order.precept` shows in-progress work, parts approval, and completion overruns.

That is all good.

The gap is subtler: many current samples are still **cleaner than the real world**. External research keeps pointing to the same realism boosters that are still underrepresented:

- explicit **request-more-information** loops
- **partial approval** or approved-with-conditions paths
- **deadline / urgency** distinctions
- formal **appeal / reconsideration / representment** stages
- richer **exception queues** and evidence collections
- **post-decision fulfillment** with its own gates
- **reopen** paths when new evidence arrives

The sample library does not need every sample to become heavier. But the next realism pass should add a few samples that look unmistakably like live operating work instead of idealized process diagrams.

---

## 7. What makes a sample feel credible rather than toy-like

### Credibility signals

A realism-focused Precept sample should usually contain several of these:

1. **A named business artifact** with stakes: prior auth request, access grant, dispute case, benefits application, invoice exception.
2. **At least two distinct actors** encoded implicitly in the workflow: requester and reviewer, claimant and adjuster, merchant and issuer, manager and compliance.
3. **A document or evidence loop** instead of one-shot approval.
4. **A rule that is policy-shaped, not programmer-shaped** — thresholds, eligibility, missing documents, risk score, SoD conflict, amount caps, receipt matching.
5. **A meaningful post-approval state** — payout, provisioning, payment, issuance, recovery, shipment, closure review.
6. **A denial or rejection reason that sounds like a business notice**, not a developer exception.
7. **One place where inspection matters** — a sample should invite `inspect` use because multiple outcomes are plausibly available.
8. **At least one constraint that makes an impossible state visibly impossible**.

### Toy smells

A sample starts feeling fake when:

- it is really just `Draft -> Approved -> Closed`
- approval exists without evidence, routing, or threshold logic
- the field set is generic (`Name`, `Description`, `StatusNote`) rather than domain-bearing
- every path is forward-only with no bounce-back, exception, or reconsideration
- the workflow ends at the decision and ignores fulfillment
- the messages sound like validation errors instead of business policy
- the sample teaches syntax but not domain pressure

---

## 8. Recommended next sample lanes

### Best flagship realism candidates

1. **PriorAuthorizationRequest**  
   Best mix of regulated rules, evidence loops, urgency, denial reasons, and appeal potential.

2. **AccessGrantReview** or **PrivilegedRoleRequest**  
   Cleanly expresses approval routing, compliance checks, attestation, and revocation.

3. **ChargebackDispute**  
   Distinctive, time-bound, evidence-heavy, and sharper than a generic refund flow.

4. **KycOnboardingCase**  
   Strong fit for Precept's "one file, every rule" promise.

### Strong supporting-library candidates

- **BenefitsEligibilityCase**
- **InvoiceExceptionReview**
- **SecurityIncidentCase**
- **InsuranceClaimReopen**
- **WarrantyInspectionDecision**

---

## 9. Recommendation to the team

If the goal is sample realism, the next wave should bias toward **case-centric, evidence-bearing, exception-rich workflows** rather than additional straight-line approvals.

Precept looks most differentiated when a reader can feel three things at once:

- the entity has a real lifecycle,
- the rules have institutional consequences,
- and the contract makes the whole thing inspectable.

That is where the category claim lands. Not in a toy ticket. In a governed file.

---

## 10. Source index

### Core official / primary references

- CMS, *Interoperability and Prior Authorization Final Rule (CMS-0057-F)* — https://www.cms.gov/priorities/burden-reduction/overview/interoperability/policies-regulations/cms-interoperability-prior-authorization-final-rule-cms-0057-f
- Microsoft, *What are lifecycle workflows?* — https://learn.microsoft.com/en-us/entra/id-governance/what-are-lifecycle-workflows
- Temporal, *Workflows* — https://docs.temporal.io/workflows
- AWS, *What is Step Functions?* — https://docs.aws.amazon.com/step-functions/latest/dg/welcome.html
- Stripe, *How disputes work* — https://docs.stripe.com/disputes/how-disputes-work
- SWIFT, *The KYC process explained* — https://www.swift.com/risk-and-compliance/know-your-customer-kyc/kyc-process
- NIST, *Incident Response project* — https://csrc.nist.gov/projects/incident-response

### Domain and operations references

- Appian, *Developing a Case Management Process in the Public Sector* — https://appian.com/blog/acp/public-sector/case-management-process-public-sector
- American Medical Association, *Prior authorization practice resources* — https://www.ama-assn.org/practice-management/prior-authorization/prior-authorization-practice-resources
- American Hospital Association, *Prior Authorization Final Rule Will Improve Patient Access* — https://www.aha.org/news/blog/2024-02-15-prior-authorization-final-rule-will-improve-patient-access-alleviate-hospital-administrative-burdens
- FirstAML, *Workflow-first compliance* — https://www.firstaml.com/resources/workflow-first-compliance-structuring-amlctf-for-real-world-delivery/
- Fluix, *Insurance Claim Process* — https://fluix.io/workflows/insurance-claims-process
- Five Sigma, *Optimizing the Claims Process Starting with FNOL* — https://fivesigmalabs.com/blog/optimizing-the-claims-process-starting-with-fnol/
- Terra, *Why Workflow Bottlenecks Persist in Claims* — https://terra.insure/blog/claims-workflow-bottlenecks-automation-tools/
- Procurify, *Purchase Approval Workflows* — https://www.procurify.com/blog/purchase-approval-workflows/
- Tipalti, *Invoice Approval Workflow* — https://tipalti.com/resources/learn/invoice-approval-workflow/
- Adyen, *Dispute flow* — https://docs.adyen.com/risk-management/understanding-disputes/dispute-process-and-flow/
- Mastercard, *Chargebacks Made Simple Guide* — https://www.mastercard.us/content/dam/public/mastercardcom/na/global-site/documents/chargebacks-made-simple-guide.pdf
- Creately, *The Complete Case Management Process Guide* — https://creately.com/guides/case-management-process/
