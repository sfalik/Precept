# EAM / Maintenance / APM Domain Benchmarks for Precept Samples

**Author:** J. Peterman (Brand/DevRel)  
**Date:** 2026-07-19  
**Status:** Research artifact — input for sample-realism initiative  
**Depends on:** `peterman-enterprise-ecosystem-benchmarks.md`, `peterman-entity-centric-benchmarks.md`, `frank-state-graph-taxonomy-and-insurance-realism.md`, `steinbrenner-state-graph-taxonomy-planning.md`  
**Philosophy flag:** Yes — this research identifies EAM/maintenance as a Tier 1 domain for Precept. The domain's entity-plus-workflow duality, its dense field governance, and its safety-critical authorization patterns make it a natural proof point for Precept's positioning as a domain integrity engine, not just a workflow tool.

---

## 0. Why this research exists

Shane has direct EAM/APM background from his time at Ivara (now Bentley Asset Reliability / APM). The maintenance domain is one of the few enterprise verticals where **every Precept modeling lane** — workflow contracts, hybrid contracts, and stateless entity contracts — has a natural, credible home. This research grounds that claim in external evidence and identifies the specific lifecycle and entity patterns worth modeling as Precept samples.

The existing `maintenance-work-order.precept` sample is a facilities-level work order: intake → schedule → complete. It is classified as a **Branching (B)** shape in Frank's taxonomy. It carries no failure coding, no materials reservation, no permit-to-work gates, no condition-based triggering, and no field-per-state editability — all of which are standard in enterprise EAM platforms. This pass identifies what a serious maintenance domain suite would look like.

---

## 1. Platforms and standards examined

### 1.1 EAM / CMMS platforms (5)

| Platform | What I studied | Why it matters |
|---|---|---|
| **SAP PM / S/4HANA EAM** | Work order lifecycle (CRTD → REL → PCNF → CNF → TECO → CLSD), maintenance plans (time-based, counter-based, strategy plans), equipment BOM, spare parts reservation, notification → order flow | The dominant enterprise EAM platform. Its status model is the industry reference for work order governance. |
| **IBM Maximo** | Work order statuses (WAPPR → APPR → WMATL → WSCH → INPRG → COMP → CLOSE), PM templates with meter/time triggers, cron-task auto-generation, asset hierarchy, failure reporting | The second-most-deployed EAM globally. Maximo's explicit "waiting" substates (WMATL, WSCH) are structurally distinct from SAP's model. |
| **Bentley AssetWise APM (formerly Ivara EXP)** | Condition monitoring, P-F interval alerting, asset health scoring, reliability-centered maintenance strategy selection, integration with SAP/Maximo for work order generation | The APM layer that sits above EAM. Ivara's core innovation was formalizing the path from condition signal to maintenance action — exactly the inspection-finding → work-order generation flow. |
| **Infor EAM / CloudSuite** | Work order lifecycle, preventive maintenance scheduling, calibration management, fleet management patterns | Enterprise EAM with strong calibration and fleet extensions. Confirms the universal work order status model. |
| **Fiix / Limble / eMaint (cloud CMMS)** | Simplified work order lifecycles, PM scheduling, parts tracking, condition-based triggers | Mid-market CMMS platforms. Their simplification of the SAP/Maximo model shows which lifecycle elements are truly universal versus platform-specific. |

### 1.2 Standards and specifications (5)

| Standard | What it contributes |
|---|---|
| **ISO 14224** (Petroleum — reliability and maintenance data) | The canonical failure classification taxonomy: **symptom → failure mode → failure cause/mechanism → failure effect**. Pre-defined code libraries for pumps, compressors, valves, etc. The standard that makes failure coding a structured business artifact rather than free text. |
| **ISO 55000/55001/55002** (Asset management) | Lifecycle-stage framework: acquisition → operation → maintenance → condition assessment → renewal/disposal. Criticality ranking formula: Consequence of Failure × Likelihood of Failure. Maintenance strategy selection flows from condition + criticality assessment. |
| **MIMOSA OSA-EAI / CRIS** | Open data exchange standard for asset management. The Common Relational Information Schema (CRIS) explicitly models work orders, assets, personnel, conditions, measurements, and diagnostics as interrelated entities. Vendor-neutral reference model for what a work order entity *contains*. |
| **API 510/570/580/581** | Pressure vessel and piping inspection codes. API 580/581 formalize **Risk-Based Inspection (RBI)** — shifting inspection intervals from fixed schedules to probability × consequence models. Inspection findings feed back into maintenance strategy. |
| **OSHA 29 CFR 1910.147 (LOTO)** | Lockout/Tagout regulatory standard. Defines the procedural requirements for energy isolation before maintenance work. The regulatory anchor for Permit-to-Work authorization gates. |

### 1.3 Supplementary sources

| Source | What it adds |
|---|---|
| SAP Community blogs on PM order cycle | Detailed status-transition rules, user status overlays, business transaction control per status |
| Maximo Mastery / Maximo Secrets / DoubtMaximo blogs | Practitioner-level work order status explanations, meter-based PM hierarchies |
| Accruent / BCG / Petrofac / ABB / Emerson TAR guides | Shutdown/Turnaround/Outage lifecycle phases, scope freeze discipline, readiness verification |
| Swain Smith Reliability Event Codes | Free ISO 14224-aligned failure code library — shows how failure classification looks as structured data |
| Power-mi / Fiix / Innius CBM guides | P-F curve application to inspection and work order generation workflows |
| IntelliPERMIT / ToolKitX / SafetySpace PTW guides | Permit-to-Work lifecycle states and LOTO integration patterns |
| SAP Learning / SAP Asset Manager | EAM inspection checklist configuration, mobile inspection execution |
| Inspectioneering / RINA / ICORR | RBI methodology, asset integrity management, inspection planning integration |

---

## 2. Enterprise work order lifecycle — the universal model

### 2.1 Cross-platform work order status comparison

Every enterprise EAM platform implements a variant of the same fundamental lifecycle. The differences are in naming and substatus granularity, not in structure.

| Phase | SAP PM | IBM Maximo | Universal pattern |
|---|---|---|---|
| **Created** | CRTD | WAPPR | Work identified but not yet approved for execution |
| **Approved / Released** | REL | APPR | Authorized to proceed; materials can be reserved, resources allocated |
| **Waiting** | (user status) | WMATL, WSCH | Blocked on materials or scheduling — structurally distinct from "approved" |
| **In Progress** | (implicit via confirmations) | INPRG | Work actively being performed; actual start recorded |
| **Partially Confirmed** | PCNF | — | Some operations complete, others remain; may generate follow-up corrective orders |
| **Confirmed / Completed** | CNF → TECO | COMP | All physical work finished; technical closeout |
| **Closed** | CLSD | CLOSE | All financial/administrative activity finalized; record is historical |
| **Cancelled** | (user status) | CAN | Abandoned before completion; restrictions if actuals already posted |

### 2.2 What the universal model reveals for Precept

**Key structural observations:**

1. **The work order lifecycle is not a simple approval ladder.** It has explicit "waiting" substates (materials, scheduling), partial confirmation, and a two-stage close (technical vs. administrative). This maps to Frank's taxonomy shapes: it is at minimum a **Diamond (D)** with waiting-state branches, and in SAP's case a **Multi-Loop (ML)** because partial confirmation can generate follow-up corrective orders that loop back to Created.

2. **Field editability is status-governed.** SAP's "business transaction control" locks specific transactions per status — you cannot post goods movements against a CRTD order, cannot add confirmations after TECO. This is precisely Precept's `edit` and `in State assert` model.

3. **The notification-to-order flow is a distinct entity concern.** In SAP, a Maintenance Notification (condition finding, breakdown report, malfunction report) is a separate entity that gets "converted" to a Work Order. This is a cross-entity orchestration concern — relevant for Precept's single-entity boundary, because the *notification* and the *order* each have their own lifecycle.

4. **Materials reservation is a field-level governance concern.** When a work order is Released, materials are reserved from inventory. This is a field-state-dependent side effect — the kind of thing `edit` blocks and computed fields would govern.

---

## 3. Domain patterns identified — Precept sample candidates

### 3.1 Workflow contracts (states + events + transitions)

| Pattern | Lifecycle shape | External anchors | Precept fit | Priority |
|---|---|---|---|---|
| **Corrective Maintenance Work Order** | Diamond + Multi-Loop (D + ML) — waiting states, partial confirmation, follow-up generation | SAP PM (CRTD → REL → PCNF → TECO → CLSD), IBM Maximo (WAPPR → APPR → WMATL → INPRG → COMP → CLOSE), MIMOSA CRIS work order entity | ★★★★★ — flagship rewrite of existing `maintenance-work-order.precept`. Adds failure coding, materials readiness gates, partial completion, two-stage close. | **P0 — flagship** |
| **Permit-to-Work** | Authorization-Gate (AG) — explicit authorization state before work execution | OSHA 29 CFR 1910.147, IntelliPERMIT, ToolKitX, SafetySpace, ABB/Emerson | ★★★★★ — fills the **AG shape gap** in the portfolio. Request → Risk Assessment → Isolation → Verification → Authorization → Execution → De-isolation → Closure. Safety-critical domain with real authorization semantics. | **P0 — new shape** |
| **Inspection Finding** | Branching + conditional escalation (B with conditional Diamond) — finding can close as acceptable, trigger follow-up work order, or escalate to shutdown | API 510/570/580/581, SAP EAM inspection checklists, Inspectioneering RBI methodology | ★★★★☆ — strong inspection finding lifecycle: Recorded → Assessed → Acceptable / Requires Action / Critical. Guards on severity threshold and risk score. | **P1** |
| **Shutdown / Turnaround Work Scope Item** | Linear with scope-freeze gate (L + AG variant) — item progresses through scoping → frozen → planned → executed → verified | Accruent, BCG, Petrofac, ABB, Emerson TAR guides | ★★★☆☆ — interesting but may overlap with authorization-gate shape. Better as a supporting sample than flagship. | **P2** |

### 3.2 Hybrid contracts (light lifecycle + dense field governance)

| Pattern | Lifecycle shape | External anchors | Precept fit | Priority |
|---|---|---|---|---|
| **Preventive Maintenance Schedule** | Lifecycle-light (H1): Active → Suspended → Retired, with dense scheduling fields | SAP PM maintenance plans (time/counter/strategy), IBM Maximo PM templates, Fiix/Limble PM configuration | ★★★★☆ — the PM schedule is a governed record with a simple status model but dense field constraints: frequency type (time/counter/combined), trigger thresholds, compliance windows, next-due-date calculation rules. Ideal hybrid sample. | **P1** |
| **Asset Record** | Lifecycle-light (H1): Commissioned → Active → Suspended → Decommissioned → Disposed | ISO 55000 asset lifecycle, SAP equipment master, Maximo asset application, MIMOSA CRIS asset entity | ★★★★☆ — the asset itself has a light lifecycle (mostly "Active") but dense field governance: criticality ranking, condition score, functional location, parent/child hierarchy references, nameplate data, installation date, warranty expiry. | **P1** |

### 3.3 Stateless / entity contracts (fields + invariants + editability, no states)

| Pattern | External anchors | Precept fit | Priority |
|---|---|---|---|
| **Failure Classification Record** | ISO 14224 failure taxonomy (symptom → mode → cause → mechanism → effect), Swain Smith RECs, MIMOSA CRIS | ★★★★★ — a failure classification is a pure data contract: structured fields with enumerated vocabularies, cross-field validation (symptom must be compatible with equipment class), and business-toned because messages. Perfect entity-lane candidate if #22 ships. | **P1 (contingent on #22)** |
| **Spare Part / Material Master** | SAP material master, Maximo item master, MIMOSA CRIS, ERP reference data patterns | ★★★☆☆ — interesting but may overlap with the general Vendor/Product entity lane from the entity-centric benchmarks. | **P2 (contingent on #22)** |

---

## 4. Deep-dive: Corrective Maintenance Work Order (flagship rewrite)

### 4.1 Why the current sample needs a complete rewrite

The current `maintenance-work-order.precept` describes itself as "a facilities work order from intake through scheduling and completion." Against the realism test from `frank-language-and-philosophy.md`, it fails on multiple criteria:

| Realism criterion | Current sample | Enterprise reality |
|---|---|---|
| Domain expert recognition | "Facilities work order" — recognizable but generic | Enterprise maintenance professionals expect failure coding, work type classification, priority derived from criticality, materials readiness |
| Guard conditions encode real business rules | `!Urgent \|\| PartsApproved` — one meaningful guard | SAP/Maximo enforce: cannot release without planner assignment, cannot start without materials staged, cannot technically complete without all operations confirmed, cannot close without cost settlement |
| Exception paths and rework | Single reject on hours overage | Partial confirmation generates follow-up orders; work orders can be re-opened from TECO if defects found; cancellation requires justification and has financial implications |
| Field-per-state editability | `in Draft edit Location, IssueSummary, Urgent` — one state | SAP controls field editability per status: planning-only fields locked after release, confirmation fields locked after TECO, cost fields locked after CLSD |
| Institutional vocabulary | Generic (RequesterName, Location, IssueSummary) | EAM vocabulary: FunctionalLocation, EquipmentId, WorkType (CM/PM/CBM), PriorityCode, FailureClass, MaintenancePlant, PlannerGroup |
| Evidence/history accumulation | CompletionNote — single text field | Real work orders accumulate: failure codes, operation confirmations, material postings, measurement readings, technician time entries |

### 4.2 What the rewritten sample should carry

A credible corrective maintenance work order sample should include:

**Fields (enterprise-grade vocabulary):**
- `EquipmentId`, `FunctionalLocation`, `MaintenancePlant` — asset identification
- `WorkType` — classification (Corrective, Emergency, Rework)
- `PriorityCode` — derived from criticality (1-Critical through 4-Routine)
- `FailureSymptom`, `FailureMode`, `FailureCause` — ISO 14224-aligned coding
- `PlannerGroup`, `AssignedTechnician` — responsibility
- `EstimatedDuration`, `ActualDuration` — labor tracking
- `MaterialsRequired`, `MaterialsStaged` — readiness gate (boolean or count)
- `PermitRequired`, `PermitApproved` — safety authorization link
- `CompletionNotes`, `CostSettled` — close-out evidence

**States (SAP/Maximo-informed):**
- `Created` → `Released` → `WaitingMaterials` → `InProgress` → `TechnicallyComplete` → `Closed`
- Branch: `Cancelled` reachable from Created, Released, WaitingMaterials
- Potential loop: TechnicallyComplete → Reopened → InProgress (defect found after completion)

**Guards and invariants (real business rules):**
- Cannot release without PlannerGroup assigned
- Cannot start without MaterialsStaged (or MaterialsRequired == false)
- Cannot start Priority 1/2 work without PermitApproved (when PermitRequired)
- Cannot technically complete without FailureMode and FailureCause coded
- Cannot close without CostSettled
- Estimated duration must be positive; actual duration must be >= 0

**Lifecycle shape:** Diamond (D) with potential Multi-Loop (ML) for the reopen path. Fills the under-represented Diamond shape in the portfolio.

### 4.3 FUTURE(...) annotations expected

The rewritten sample will likely need:
- `FUTURE(set<string>)` for material line items if collection types haven't shipped
- `FUTURE(computed)` for priority derivation from criticality × urgency if computed fields aren't available
- `FUTURE(entity-ref)` for EquipmentId pointing to an Asset precept

---

## 5. Deep-dive: Permit-to-Work (new shape — Authorization Gate)

### 5.1 Why this sample matters for the portfolio

The Permit-to-Work lifecycle is the single best candidate for filling the **Authorization Gate (AG)** shape gap identified in Frank's taxonomy. It is:

- **Universally recognized** across asset-intensive industries (oil & gas, petrochemical, power, manufacturing, mining)
- **Safety-critical** — authorization is not a business convenience but a regulatory requirement (OSHA 1910.147)
- **Structurally distinct** from approval workflows — the permit is an entity the work order *depends on*, not a stage within the work order itself
- **Dense with field governance** — isolation points, energy types, hazard classifications, verification checkpoints

### 5.2 Lifecycle (external evidence)

From the PTW research (IntelliPERMIT, ToolKitX, SafetySpace, OSHA, CSTC Safety):

| State | Description | Key governance |
|---|---|---|
| **Requested** | Work scope defined; hazards identified | Must specify work description, location, hazard types |
| **RiskAssessed** | Hazards analyzed, controls specified | Risk assessment fields locked after progression |
| **IsolationApplied** | Energy sources physically isolated, LOTO applied | Isolation points enumerated; personal locks tracked |
| **IsolationVerified** | Effectiveness verified (zero-energy test) | Cannot proceed to authorization without verification |
| **Authorized** | Supervisor/area authority sign-off | **Authorization gate** — explicit event, not auto-transition |
| **WorkInProgress** | Protected work execution | Conditions monitored; scope changes require permit suspension |
| **Suspended** | Permit temporarily halted (scope change, new hazard, shift handover) | Re-authorization required to resume |
| **DeIsolated** | Isolation removed; equipment re-energized | All personal locks confirmed removed; area cleared |
| **Closed** | Final verification complete; documentation archived | Archived for audit trail |

**Lifecycle shape:** Authorization-Gate (AG) with a Single-Loop (SL) for the Suspension cycle. Fills two missing taxonomy shapes in one sample.

### 5.3 Sample implications

**Fields:**
- `WorkDescription`, `Location`, `EquipmentId` — scope
- `HazardTypes` — FUTURE(set<string>) for enumerated hazard classifications
- `IsolationPointCount`, `IsolationVerified` — physical safety verification
- `RiskLevel` — assessed severity (High/Medium/Low)
- `AuthorizedBy`, `AuthorizationTimestamp` — authorization evidence
- `PermitHolder`, `AreaAuthority` — responsibility
- `SuspensionReason` — required when entering Suspended
- `ClosureVerifiedBy` — final sign-off

**Key guards:**
- Cannot authorize without IsolationVerified
- Cannot start work with RiskLevel == "High" without additional safety officer sign-off (FUTURE: multi-approver)
- Cannot de-isolate without all personal locks confirmed removed
- Cannot close without ClosureVerifiedBy assigned
- Suspension requires SuspensionReason; resumption requires re-authorization

---

## 6. Deep-dive: Inspection Finding (condition-based maintenance bridge)

### 6.1 Why this pattern matters

The inspection finding is the bridge between condition monitoring (APM) and maintenance execution (EAM). In Bentley AssetWise / Ivara EXP terms, this is where the P-F curve becomes actionable: a condition signal or inspection observation generates a finding that must be assessed, classified, and either accepted or escalated to a work order.

### 6.2 Lifecycle

| State | Description | Key governance |
|---|---|---|
| **Recorded** | Inspector documents observation: location, equipment, measurement/reading, visual condition | Must have EquipmentId, InspectionType, FindingDescription |
| **Assessed** | Engineer reviews finding, assigns severity and risk score | SeverityLevel and RiskScore required before progression |
| **Acceptable** | Finding within tolerance — no action required; archived | Terminal state — but must carry AssessedBy and AcceptanceRationale |
| **ActionRequired** | Finding exceeds threshold — follow-up work order to be generated | WorkOrderReference required (FUTURE: entity-ref) |
| **Critical** | Finding indicates imminent failure or safety risk — immediate shutdown/containment | Escalation fields: EmergencyContact, ShutdownRequired |
| **Resolved** | Follow-up work completed and verified | ResolutionVerifiedBy, ResolutionDate required |

**Lifecycle shape:** Branching (B) with conditional Diamond — Assessed branches three ways, ActionRequired and Critical both reconverge at Resolved. Adds structural depth beyond simple B shape.

### 6.3 Ivara / Bentley AssetWise connection

This sample is directly grounded in the Ivara/Bentley APM model:
- **P-F interval**: The finding captures a point on the P-F curve between potential failure detection and functional failure
- **Health scoring integration**: Severity and risk scoring mirror AssetWise's asset health index methodology
- **Work order generation**: The ActionRequired → WorkOrderReference flow mirrors the APM-to-EAM handoff that Ivara/Bentley pioneered

---

## 7. Precept fit assessment — why EAM is a Tier 1 domain

### 7.1 Domain-fit evaluation (Frank's five commitments)

| Commitment | EAM/Maintenance assessment |
|---|---|
| **The domain noun is a governed entity with a meaningful lifecycle** | ★★★★★ — Work Order, Permit-to-Work, Inspection Finding, PM Schedule, Asset Record all have documented, multi-platform-validated lifecycles with explicit status governance |
| **The business rules are non-trivial and domain-specific** | ★★★★★ — Failure coding requirements, materials readiness gates, safety authorization, risk-based inspection thresholds, partial confirmation logic, cost settlement prerequisites |
| **The lifecycle has branches, exceptions, or rework** | ★★★★★ — Waiting states, partial confirmation loops, permit suspension/resumption, finding escalation branches, work order reopening |
| **Field editability varies by lifecycle stage** | ★★★★★ — SAP PM's business transaction control is the textbook example of field-per-state governance. Every EAM platform locks fields by status. |
| **The domain is recognizable to a broad professional audience** | ★★★★☆ — Maintenance professionals are a large audience; "work order" is universal. Slightly less broad than insurance or finance, but deeply credible for asset-intensive industries. |

**Total: 24/25** — This is the strongest domain-fit score of any domain studied in this research series. EAM/maintenance matches every lane Precept models: workflow, hybrid, and entity.

### 7.2 Coverage across all three contract archetypes

| Archetype | EAM candidates | Count |
|---|---|---|
| **Workflow** | Corrective Work Order, Permit-to-Work, Inspection Finding, (Turnaround Work Scope Item) | 3–4 |
| **Hybrid** | Preventive Maintenance Schedule, Asset Record | 2 |
| **Entity/Stateless** | Failure Classification Record, Spare Part Master | 2 (contingent on #22) |

No other domain studied in this research series spans all three archetypes with such natural credibility. Insurance comes close (Claim workflow + Policy lifecycle + Adjuster entity), but EAM's depth is broader.

### 7.3 Taxonomy shape coverage

| Shape | EAM sample that fills it |
|---|---|
| **D (Diamond)** — under-represented | Corrective Work Order (waiting-state branches reconverge at completion) |
| **AG (Authorization-Gate)** — **missing** | Permit-to-Work (explicit authorization state before work execution) |
| **ML (Multi-Loop)** — all toy domains | Corrective Work Order with reopen path (if modeled) |
| **SL (Single-Loop)** | Permit-to-Work suspension/resumption cycle |

The EAM domain fills or strengthens **four** taxonomy positions, including two that are currently empty across the entire portfolio.

---

## 8. Recommended sample suite

### 8.1 Priority ordering

| # | Sample | Archetype | Shape | Priority | Notes |
|---|---|---|---|---|---|
| 1 | **Corrective Maintenance Work Order** (rewrite) | Workflow | D + ML | **P0** | Replaces existing `maintenance-work-order.precept`. Adds failure coding, materials gates, two-stage close, field-per-state editability. |
| 2 | **Permit-to-Work** | Workflow | AG + SL | **P0** | New sample. Fills the Authorization-Gate gap. Safety-critical domain. |
| 3 | **Inspection Finding** | Workflow | B + conditional D | **P1** | New sample. Bridges APM → EAM. Grounded in Ivara/Bentley heritage. |
| 4 | **Preventive Maintenance Schedule** | Hybrid | H1 | **P1** | New sample. Dense field governance with minimal lifecycle. |
| 5 | **Asset Record** | Hybrid | H1 | **P2** | New sample. Light lifecycle, dense entity fields. May overlap with general entity-lane planning. |
| 6 | **Failure Classification Record** | Entity | E3 | **P2** | New sample. ISO 14224-grounded. Contingent on #22 (stateless precepts). |

### 8.2 Corpus impact

The current sample count is 21. Adding 2 net-new P0 samples (Permit-to-Work + Corrective Work Order rewrite replaces existing) brings the count to 23. Adding the P1 pair (Inspection Finding + PM Schedule) reaches 25. This is well within the 30–36 operating range from the ceiling decision.

### 8.3 Relationship to existing portfolio planning

- The Corrective Work Order rewrite and Permit-to-Work sample directly satisfy Steinbrenner's missing workflow lanes: **authorization-gated operations** and **deepened enterprise Diamond shape**.
- The Inspection Finding sample adds the condition-monitoring bridge that no other domain in the portfolio provides.
- The Preventive Maintenance Schedule sample adds the **lifecycle-light governed record (H1)** shape that Steinbrenner's taxonomy identified as a missing hybrid lane.
- Shane's Ivara/Bentley background gives these samples an authenticity advantage that no other domain lane can claim from this team.

---

## 9. Source index

### EAM platforms

| Source | URL |
|---|---|
| SAP PM Maintenance Order Cycle User Manual | https://erp-docs.com/2770/sap-pm-maintenance-order-cycle-user-manual-introduction-and-key-process-steps/ |
| SAP PM Work Orders Tutorial | https://pmmodule.blogspot.com/p/work-orders.html |
| SAP Community — Understanding Work Order System Status | https://community.sap.com/t5/product-lifecycle-management-q-a/understanding-work-order-system-status/qaq-p/505124 |
| SAP Community — TECO and CLSD | https://community.sap.com/t5/enterprise-resource-planning-q-a/pm-work-order-teco-and-clsd/qaq-p/8723119 |
| SAP Community — Automatic Corrective Maintenance Creation for PCNF | https://community.oxmaint.com/discussion-forum/implementing-automatic-corrective-maintenance-creation-in-sap-for-pm-orders-with-pcnf-status |
| IBM — Work Order Statuses (Maximo Manage) | https://www.ibm.com/docs/en/masv-and-l/maximo-manage/cd?topic=overview-work-order-statuses |
| Maximo Mastery — Work Order Statuses Explained | https://maximomastery.com/blog/2026/02/maximo-work-order-statuses-explained/ |
| DoubtMaximo — Work Order Statuses | https://doubtmaximo.blogspot.com/2012/10/work-order-statuses.html |
| University of Delaware — Maximo Work Order Statuses SOP | http://web.facilities.udel.edu/docs/Sharepoint/MO/SOPs/UDFREASMOMMS002UDMaximoWorkOrderStatuses.pdf |

### APM / Reliability

| Source | URL |
|---|---|
| Bentley AssetWise Reliability | https://www.bentley.com/software/assetwise-reliability/ |
| Bentley AssetWise Asset Health Monitoring (PDS) | https://www.bentley.com/wp-content/uploads/PDS-AssetWise-Health-Monitoring-LTR-EN-LR.pdf |
| Bentley Asset Performance Software | https://www.bentley.com/software/asset-performance/ |
| Reliabilityweb — Bentley's Acquisition of Ivara | https://reliabilityweb.com/news/article/bentleys_acquisition_of_ivara_redefines_asset_performance_management |
| SAP Community — CBM with SAP APM | https://community.sap.com/t5/supply-chain-management-blog-posts-by-sap/condition-based-maintenance-with-sap-asset-performance-management-sap-apm/ba-p/13548535 |
| Power-mi — P-F Curve in CBM | https://power-mi.com/content/p-f-curve-cornerstone-condition-based-maintenance |
| Fiix — Complete CBM Guide | https://fiixsoftware.com/blog/effective-condition-based-maintenance/ |

### Standards

| Source | URL |
|---|---|
| ISO 14224:2016 | https://www.iso.org/standard/64076.html |
| Ecesis — ISO 14224 Failure Codes Guide | https://www.ecesis.net/Preventive-Maintenance-Software/ISO-14224-Failure-Codes.aspx |
| Swain Smith — Free Reliability Event Codes (ISO 14224-aligned) | https://swainsmith.com/reliability-event-codes/ |
| f7i.ai — Failure Modes and Causes Taxonomy | https://f7i.ai/blog/failure-modes-and-causes-how-to-build-a-data-taxonomy-for-enterprise-reliability |
| ISO 55000 Asset Management | https://iso-library.com/standard/55000/ |
| Asset Management Standards | https://www.assetmanagementstandards.com/ |
| MIMOSA — Open Standards for Physical Asset Management | https://www.mimosa.org/ |
| PHM Society — Implementing MIMOSA Standards | https://papers.phmsociety.org/index.php/phme/article/download/1647/609 |

### Permit-to-Work / Safety

| Source | URL |
|---|---|
| IntelliPERMIT — LOTO & PTW Unified Approach | https://www.intellipermit.com/blog/lockout-tagout-loto-permit-to-work/ |
| ToolKitX — PTW Lifecycle & Digital Best Practices | https://toolkitx.com/blogsdetails.aspx?title=Optimizing-the-Permit-to-work-(PTW)-process:-a-practical-guide |
| The HSE Coach — Step-by-Step PTW | https://thehsecoach.com/permit-to-work-system-2/ |
| SafetySpace — Practical PTW Guidance | https://safetyspace.co/work-to-permit |
| SafetyMint — Isolation & PTW Integration | https://www.safetymint.com/blog/isolation-management-ptw-integration/ |
| CSTC Safety — LOTO Step-by-Step | https://www.cstcsafety.com/blog/blog-series-isolation-of-hazardous-energy-lockouttagout-part-2 |

### Inspection / RBI

| Source | URL |
|---|---|
| Technical Toolboxes — Advanced API 510/570 Inspection | https://technicaltoolboxes.com/enhancing-asset-integrity-advanced-api-510-570-inspection-techniques/ |
| Inspectioneering — RBI Introduction | https://inspectioneering.com/feature/mipi/rbi/intro |
| RINA — Asset Integrity, RBI, IOW | https://www.rina.org/en/asset-integrity-rbi-iow-inspection-planning |
| SAP Learning — Managing Inspection Checklists | https://learning.sap.com/learning-journeys/exploring-functions-and-innovations-in-sap-s-4hana-asset-management/managing-inspection-checklists |

### Shutdown / Turnaround

| Source | URL |
|---|---|
| Accruent — 5 Phases of Plant Turnaround | https://www.accruent.com/resources/blog-posts/understanding-5-phases-plant-turnaround-process |
| BCG — End-to-End Approach for World Class TAR | https://web-assets.bcg.com/51/fd/e01a200d415098c2a2bb42dba405/an-end-to-end-approach-for-world-class-turnaround-maintenance.pdf |
| ABB — Turnaround Plant Services | https://library.e.abb.com/public/47e278d25b070dcc8525785a00689097/Turnaround%20Plant%20Services%20Article.pdf |
| Emerson — Shutdowns, Turnarounds & Outages Playbook | https://www.emerson.com/documents/automation/playbook-shutdowns-turnarounds-outages-en-en-10571180.pdf |
| SAP Community — Optimizing STO Management | https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-sap/optimizing-shutdown-turnaround-and-outage-management-with-sap-sto360-by/ba-p/14017169 |

### Materials / Spare Parts

| Source | URL |
|---|---|
| SAP Community — Spare Parts Management in PM | https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-members/spare-parts-management-in-sap-plant-maintenance/ba-p/13290453 |
| Locus IT — SAP PM Spare Parts & Inventory | https://locusit.com/learning/erp-corporate-trainings/sap-pm-spare-parts-and-inventory-management/ |
| ERPGREAT — Equipment BOM in SAP PM | https://www.erpgreat.com/plant/equipment-bill-of-material-in-sap-pm.htm |
| IBM — Preventive Maintenance (Maximo) | https://www.ibm.com/docs/en/masv-and-l/maximo-manage/cd?topic=module-preventive-maintenance |
| Maximo Secrets — Meter-Based PMs and Hierarchies | https://maximosecrets.com/2023/04/05/meter-based-pms-and-pm-hierarchies-2/ |

---

## 10. Conclusions

1. **EAM / Maintenance is a Tier 1 domain for Precept samples.** It scores 24/25 on Frank's domain-fit evaluation and spans all three contract archetypes (workflow, hybrid, entity). No other domain in the research series matches this breadth.

2. **The existing `maintenance-work-order.precept` should be completely rewritten** to carry enterprise-grade vocabulary (ISO 14224 failure coding, SAP/Maximo-informed status model, materials readiness gates, two-stage close, field-per-state editability). The current sample is a facilities demo that would not be recognized by an enterprise maintenance professional.

3. **Permit-to-Work is the highest-value new sample** for portfolio structure. It fills the Authorization-Gate (AG) taxonomy shape that is completely absent from the corpus. It is safety-critical, universally recognized in asset-intensive industries, and carries real regulatory backing (OSHA 1910.147).

4. **Inspection Finding bridges APM and EAM** — the condition-monitoring-to-maintenance-action flow that Ivara/Bentley pioneered. This sample gives the team a personal credibility anchor through Shane's background.

5. **Preventive Maintenance Schedule is the ideal hybrid sample** for the maintenance domain — minimal lifecycle, maximum field governance. It proves that Precept's entity-centric model works for records that are mostly about data integrity, not state transitions.

6. **Failure Classification Record is a strong entity-lane candidate** contingent on #22 (stateless precepts). ISO 14224 provides the external anchor. Structured failure coding as a governed data contract — symptom, mode, cause, mechanism, effect — is precisely the kind of business artifact that should feel like authored policy, not programmer validation.

7. **The EAM suite fills four taxonomy positions** including two that are currently empty across the entire portfolio (AG, D strengthened). This is the best structural return of any domain lane studied.
