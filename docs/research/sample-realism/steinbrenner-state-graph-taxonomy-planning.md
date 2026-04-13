# Steinbrenner State-Graph / Lifecycle-Shape Taxonomy for Sample Planning

Date: 2026-04-08  
Owner: Steinbrenner (PM)  
Status: Research artifact — planning guidance  
Purpose: Build a reusable taxonomy for sample planning that distinguishes workflow, entity/stateless, and hybrid contracts; then turn that taxonomy into concrete lane, rewrite, and sequencing decisions.  
Depends on: `steinbrenner-sample-portfolio-plan.md`, `steinbrenner-sample-ceiling-plan.md`, `frank-enterprise-platform-and-research-gaps.md`, `frank-entity-modeling-addendum.md`, `peterman-entity-centric-benchmarks.md`, `george-current-sample-audit.md`, `.squad/decisions.md`  
Philosophy flag: **Yes.** This artifact applies the active directive that Precept is entity-first, states are optional, workflow is only one lane, and sample planning must reserve room for workflow, stateless/entity, and hybrid contracts.

## Executive conclusions

1. Sample planning should use a **two-axis taxonomy**, not a flat domain list:
   - **Axis A: contract archetype** — workflow, hybrid, or stateless/entity.
   - **Axis B: lifecycle shape** — the actual graph pattern or zero-state entity form.
2. The current corpus is still overweight on one shape: **intake → review → decision**. New work should target missing shapes before adding more domains that collapse back into that same graph.
3. The next serious sample wave should add four missing workflow lanes first: **authorization-gate**, **containment/remediation**, **appeal/re-review**, and **active-life amendment/renewal**.
4. The portfolio must also reserve durable capacity for **4-6 stateless/entity samples** once #22 is scheduled or shipped: **master data**, **reference data**, and **domain-rule contracts**.
5. Hybrid contracts need their own lane. They are not “small workflows” and not “fake stateless samples.” They are lifecycle-light records with dense field governance.

---

## 1. The reusable taxonomy

### 1.1 Axis A — Contract archetype

| Archetype | States? | What it proves | Planning rule |
|---|---|---|---|
| **Workflow contract** | Yes | Precept governs a record through a meaningful lifecycle with inspectable event routing and invalid-transition prevention. | Use for flagship case, claims, approval, and operational samples. |
| **Hybrid contract** | Minimal | Precept can govern a record whose main complexity is field policy, with only a light lifecycle wrapper. | Use when the business object has a simple status model but dense editability and invariant rules. |
| **Stateless/entity contract** | No | Precept is a domain-integrity language, not just a workflow DSL. Fields, invariants, and editability stand on their own. | Reserve explicit lane capacity; never fake these as thin state machines just to fill the shelf. |

### 1.2 Axis B — Lifecycle-shape family

| Shape ID | Shape family | Archetype | Graph signature | Current status | Planning implication |
|---|---|---|---|---|---|
| **E1** | Reference definition | Stateless/entity | Zero-state, tightly bounded fields, low editability | Missing until #22 | Reserve 1-2 slots for compact lookup/reference samples. |
| **E2** | Master data contract | Stateless/entity | Zero-state, editable record, dense cross-field rules | Missing until #22 | Reserve 2-3 slots for serious governed entities. |
| **E3** | Domain-rule contract | Stateless/entity | Zero-state, policy-bearing invariants or scoring/classification logic | Missing until #22 | Reserve 1 flagship slot that proves invariants are business policy, not just schema checks. |
| **H1** | Lifecycle-light governed record | Hybrid | 2-4 states around a field-heavy record (`Draft → Active → Retired`, `Active → OnHold → Closed`) | Missing as a deliberate lane | Add 2-3 samples; this is the bridge between workflow and entity planning. |
| **W1** | Intake → Review → Decision → Fulfillment | Workflow | Linear review gate with one decisive branch | Saturated | Rewrite existing anchors; do not add more of this shape right now. |
| **W2** | Evidence loop → Adjudication | Workflow | Intake, evidence gathering, decision, reopen/settle/deny | Present but shallow | Deepen `insurance-claim`; avoid net-new duplicates until the flagship is stronger. |
| **W3** | Authorization-gated execution | Workflow | Assess → Authorize → Execute → Review → Close | Missing | Highest-value new workflow lane. |
| **W4** | Containment before remediation | Workflow | Detect/Triage → Investigate → Contain → Remediate → Close | Missing | Add as a distinct operational shape, not a variant of helpdesk intake. |
| **W5** | Appeal / reconsideration loop | Workflow | Decision can re-open into formal re-review | Missing | Add as the regulated/public-sector lane. |
| **W6** | Active-life amendment / renewal | Workflow | Approval is midstream; long-lived active state with amendment/cancel/renew paths | Missing or partial | Extend current anchors and add one flagship lifecycle sample. |
| **W7** | Simple request fulfillment | Workflow | Request → Fulfillment → Confirmation → Close | Adequately covered | Keep existing files; do not spend new slots here. |

### 1.3 Why this taxonomy is the right planning lens

- It matches the active team directive in `.squad/decisions.md`: **workflow, entity/stateless, and hybrid** are co-equal planning categories.
- It prevents false breadth. A new domain name does not count as coverage if it reuses **W1** again.
- It respects the ceiling decision. In a **30-36** operating band, every added sample must earn its slot by adding a new **archetype**, **shape**, or materially stronger roadmap pressure.

---

## 2. Planning implications by lane

### 2.1 Workflow lanes to add next

These are the missing lanes that should consume the next net-new workflow slots.

| Lane | Best exemplar candidates | Why it belongs |
|---|---|---|
| **Authorization-gated operations** | `change-request`, `purchase-order-approval` | Strongest missing enterprise shape; adds explicit authority/risk routing. |
| **Containment/remediation operations** | `security-incident`, `code-enforcement-case` | Adds a genuinely different incident graph, not just another approval case. |
| **Appeal / re-review regulated case** | `benefits-application`, `building-permit`, `prior-authorization-review` | Adds formal reconsideration loops and public/regulated realism. |
| **Active-life amendment / renewal** | `insurance-policy-lifecycle`, `invoice-lifecycle`, deeper `subscription-cancellation-retention` | Proves that “approved” is not the end of the domain. |
| **Risk / verification escalation** | `customer-onboarding-kyc`, `vendor-risk-review` | Good supporting lane after the four structural gaps above land. |

### 2.2 Hybrid lanes to add deliberately

Hybrid planning is now required, not optional.

| Lane | Example sample concepts | Why it belongs |
|---|---|---|
| **Status-light governed records** | `employee-record`, `provider-credential`, `product-listing` | Simple lifecycle, heavy field governance; ideal proof that states are optional but still useful when present. |
| **Operational master records** | `vendor-onboarding-review`, `asset-record-lifecycle` | Lets one sample show both a light lifecycle and dense editability/invariant rules. |

### 2.3 Stateless/entity lanes to reserve now

Do the planning now; do the implementation when #22 is scheduled or shipped.

| Lane | Example sample concepts | Slot guidance |
|---|---|---|
| **Master data contracts** | `vendor-master-record`, `provider-directory-entry`, `employee-profile` | 2-3 slots |
| **Reference data definitions** | `payment-terms`, `risk-tier`, `service-category` | 1-2 slots |
| **Domain-rule contracts** | `credit-application-profile`, `tariff-rate-card`, `vendor-risk-profile` | 1 flagship slot |

**Rule:** do **not** fill these reserved slots with fake state machines if #22 is late. The lane should remain visibly reserved.

---

## 3. Which existing lanes should be rewritten first

The rewrite rule still stands: deepen known anchors before inventing more samples. But the rewrite order should now follow taxonomy value, not just raw familiarity.

| Rank | Sample | Why it moves first | Taxonomy effect |
|---|---|---|---|
| 1 | `it-helpdesk-ticket` | Best existing operational sample; can gain hold/escalation/reopen realism fast. | Strengthens W1 and prepares the corpus for W4-style ops shapes. |
| 2 | `loan-application` | Current portfolio anchor that stops too early. | Extends a W1 case toward W6 active-life governance. |
| 3 | `insurance-claim` | Already owns the evidence/adjudication lane. | Makes W2 credible enough to stop the corpus from looking tutorial-grade. |
| 4 | `subscription-cancellation-retention` | Compact file with room to become a real active-life contract. | Gives the portfolio a second W6 anchor with dunning/reinstatement pressure. |
| 5 | `travel-reimbursement` | Best current money/threshold anchor. | Keeps finance pressure high while structural gaps are being filled. |
| 6 | `clinic-appointment-scheduling` | Best date-realism rewrite candidate. | Maintains roadmap pressure for date support without adding another W1 sample. |
| 7 | `library-book-checkout` | Strong due-date / renewal / count realism anchor. | Supports date + integer pressure and gives the corpus a more credible long-lived record. |
| 8 | `utility-outage-report` | Public-ops sample with room for better escalation and dispatch policy. | Broadens operational/public-sector depth without creating a new lane from scratch. |

### Why this rewrite order changed

- The first four rewrites improve **shape quality**.
- The next four improve **primitive realism** (`date`, `decimal`, `choice`, richer policy).
- That is the right sequence if the goal is to make the corpus a roadmap instrument rather than a bigger shelf.

---

## 4. Highest-value sequencing

### Wave 1 — Rewrite the anchor files

Do the eight rewrites above first. This is the fastest quality gain, and it improves the existing canon instead of adding more half-credible files.

### Wave 2 — Add the four missing workflow shapes

This is the highest-value net-new set:

1. **Authorization-gated operations** — `change-request` or `purchase-order-approval`
2. **Appeal / re-review** — `benefits-application` or `building-permit`
3. **Containment / remediation** — `security-incident`
4. **Active-life amendment / renewal** — `insurance-policy-lifecycle`

These four additions matter more than any fifth intake/review sample.

### Wave 3 — Add 2-3 hybrid samples

After the workflow shape gaps land, add deliberate hybrid anchors such as:

- `employee-record`
- `product-listing`
- `vendor-onboarding-review`

This is where the product story stops sounding workflow-only even before #22 ships.

### Wave 4 — Activate the stateless/entity lane

Once #22 is scheduled or shipped, spend the reserved 4-6 slots on:

- **2-3 master data contracts**
- **1-2 reference definitions**
- **1 domain-rule contract**

### Wave 5 — Grow cautiously toward the mid-30s

Only expand beyond the near-term **30-sample** target when the canon is stable and each addition adds a new shape, a stronger lane, or a meaningful new archetype.

---

## 5. PM planning lens going forward

When a sample candidate is proposed, score it in this order:

1. **Archetype coverage** — workflow, hybrid, or stateless/entity?
2. **Shape coverage** — which `E*`, `H*`, or `W*` slot does it fill?
3. **Roadmap pressure** — which language gaps become more obvious or more compelling?
4. **Canon value** — is this a flagship sample, an extended-shelf sample, or just a teaching/control artifact?
5. **Dilution risk** — is this just another domain skin on `W1`?

If the candidate cannot answer those five questions cleanly, it should not get a slot yet.

## PM call

The new philosophy directive is real and operational now: **Precept is entity-first, states are optional, workflow is one lane, and the sample plan must show all three archetypes.** The sample roadmap should therefore stop thinking in “more workflows” and start thinking in **portfolio balance across archetype + shape**.
