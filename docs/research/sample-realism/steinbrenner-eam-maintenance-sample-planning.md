# Steinbrenner EAM / Maintenance Sample Planning

Date: 2026-04-08  
Owner: Steinbrenner (PM)  
Status: Research artifact — planning guidance  
Purpose: Place enterprise asset management / maintenance in the current sample taxonomy, identify the strongest maintenance-related sample lanes, and recommend sequencing relative to the active rewrite/addition plan.  
Depends on: `README.md`, `steinbrenner-state-graph-taxonomy-planning.md`, `steinbrenner-sample-portfolio-plan.md`, `steinbrenner-sample-ceiling-plan.md`, `peterman-additional-sample-research.md`, `frank-state-graph-taxonomy-and-insurance-realism.md`, `george-current-sample-audit.md`, `.squad/decisions.md`, public references on ISO 55001, ISO 14224, IBM Maximo, Oracle eAM, and QAD EAM  
Philosophy flag: **Yes.** This artifact applies the active directive that Precept is entity-first, states are optional, and workflow is only one lane. EAM is valuable precisely because it naturally spans workflow, hybrid, and future stateless/entity contracts.

## Executive conclusions

1. **EAM belongs in the portfolio as a multi-lane domain, not as one more service-ticket example.** Its natural coverage is **W3 authorization-gated execution**, **W4 containment/remediation**, **H1 lifecycle-light governed records**, and future **E1/E2/E3** entity lanes.
2. The existing `maintenance-work-order.precept` is the right seed, but it is currently too small and facilities-generic. It should be rewritten into a flagship **enterprise work-management** sample, not left as a side sample.
3. The first maintenance bundle should be **work order workflow + asset record companion**, with **failure-code reference data** and **preventive-maintenance template/policy** queued behind #22.
4. To keep the portfolio disciplined, maintenance should **replace** weaker generic operational slots before it adds new ones.

---

## 1. Where EAM / maintenance fits in the taxonomy

### 1.1 Best placement by contract archetype and shape

| Maintenance concept | Best archetype | Best shape | Why this is the right fit |
|---|---|---|---|
| **Maintenance work order** | Workflow | **W3** authorization-gated execution | Real enterprise work management is assess/approve/plan/schedule/execute/complete/close, with explicit holds for labor, permits, parts, or outage windows. |
| **Breakdown / corrective-maintenance response** | Workflow | **W4** containment/remediation | Serious maintenance sometimes begins with failure containment and return-to-service pressure, not just a clean planned job. |
| **Asset record lifecycle** | Hybrid now; E2 later | **H1** now; **E2** later | Asset records are field-heavy governed entities with a light lifecycle (`Draft/Active/OutOfService/Retired`) rather than deep event choreography. |
| **Preventive-maintenance plan / job plan governance** | Hybrid now; E3 later | **H1** now; **E3** later | PM programs are mostly policy-bearing templates: interval rules, trigger mode, assigned asset scope, required craft, compliance expectations. |
| **Failure/problem/cause/remedy catalog** | Stateless/entity | **E1** | This is governed reference data, not a workflow. It should remain visibly reserved for the data-only lane. |
| **Asset criticality / maintenance strategy policy** | Stateless/entity | **E3** | This is a domain-rule contract: classify what maintenance strategy and review level apply, without forcing fake states. |

### 1.2 What EAM should **not** be used for

- **Do not spend a flagship slot on a plain maintenance request.** That collapses back into **W1** intake/review, which the corpus already over-represents.
- **Do not split “field service dispatch” and “maintenance work order” unless their graph shapes differ.** One generic dispatch sample plus one maintenance work-order sample is fine only if one owns **W3** and the other owns **W4**.
- **Do not fake reference data as tiny state machines.** Failure codes, asset classes, and PM templates should wait for the stateless/entity lane if the right surface is not ready.

---

## 2. External precedent that makes this lane strong

### 2.1 What the outside world consistently models

| Source | Observation | Planning implication |
|---|---|---|
| IBM Maximo public work-order materials and Oracle eAM documentation | Work management follows a recognizably enterprise path: waiting for approval / approval, planning, execution, completion, closure. | The flagship maintenance sample should be a **true work-management lifecycle**, not a repair ticket with one assignment event. |
| Oracle eAM and QAD EAM preventive-maintenance documentation | PM templates and schedules are assigned to assets or asset classes and generate work orders from time-, meter-, or rule-based triggers. | Maintenance deserves both a **workflow sample** and a **governed template/entity companion**. |
| ISO 14224 public summaries and implementation material | Reliability/maintenance data is structured around asset hierarchy plus problem/failure, cause, consequence, and action/remedy coding. | Maintenance is a natural fit for **reference data** and **governed entity** samples, not only workflows. |
| ISO 55001 / GFMAM asset-management framing | Maintenance is part of a larger asset-management system: asset identity, hierarchy, strategy, risk, and continuous improvement all matter. | The portfolio should pair lifecycle depth with governed companion entities from the same domain. |

### 2.2 What that means for Precept sample design

The maintenance lane is strong because it naturally combines:

1. **Authority gates** — approval, scheduling, permit readiness, material readiness.
2. **Operational execution** — labor actuals, completion evidence, return-to-service decisions.
3. **Governed master/reference data** — assets, failure codes, maintenance strategies, PM templates.
4. **Enterprise recognizability** — Maximo / SAP / Oracle / Infor users immediately understand the nouns.

That is exactly the kind of domain bundle the current sample philosophy wants: **motion plus meaning**.

---

## 3. Best maintenance-related sample lanes for the corpus

### 3.1 Workflow lane — make maintenance work order the flagship

| Candidate | Archetype / shape | Recommendation | Why |
|---|---|---|---|
| **`maintenance-work-order` rewrite** | Workflow / **W3** | **Do this first.** Promote the existing sample into the enterprise maintenance flagship. | It already exists, George's audit already flags it as a realism foundation, and it fills a missing high-value shape faster than a net-new sample. |
| **`equipment-failure-response`** | Workflow / **W4** | **Add later, not first.** Use only if we still need a distinct containment/remediation anchor after the work-order rewrite. | This gives maintenance a second workflow lane without duplicating the main work-order shape. |
| **`maintenance-request`** | Workflow / W1 | **Do not prioritize.** | The corpus does not need another intake/review shell. Fold request intake into the flagship work-order sample instead. |

#### What the flagship work-order rewrite should carry

- Request intake or generation source
- work type / priority / craft / permit or access readiness
- planning/scheduling before execution
- parts or material hold logic
- actual labor and completion evidence
- completion review and formal close
- explicit link fields for **asset**, **failure code**, **job plan / PM source**, even if companion samples ship later

### 3.2 Hybrid lane — use asset and PM governance, not fake workflows

| Candidate | Archetype / shape | Recommendation | Why |
|---|---|---|---|
| **`asset-record-lifecycle`** | Hybrid / **H1** | **First hybrid maintenance companion.** | Best bridge between the entity-first directive and the work-order flagship. |
| **`preventive-maintenance-program`** | Hybrid / **H1** | **Second hybrid maintenance companion, if needed before #22.** | Shows schedule governance, activation/suspension, and assignment scope without pretending the whole sample is a workflow engine. |
| **`job-plan-governance`** | Hybrid / H1 | **Lower priority.** | Credible, but less foundational than asset identity and PM scheduling. |

#### Why `asset-record-lifecycle` should come before PM governance

The asset record gives the work-order sample its nouns: asset class, parent asset, site/location, criticality, warranty, commissioning status, retirement state. Without that governed entity, the work order risks reading like an abstract ticket again.

### 3.3 Future stateless/entity lane — reserve the real maintenance reference model

| Candidate | Archetype / shape | Recommendation | Why |
|---|---|---|---|
| **`failure-code-catalog`** | Stateless/entity / **E1** | **First maintenance data-only sample once #22 is ready.** | The cleanest reference-data proof: problem, cause, remedy, effect, maybe equipment-class applicability. |
| **`asset-master-record`** | Stateless/entity / **E2** | **Later migration or companion to the hybrid asset record.** | If #22 lands, this becomes the zero-state version of the same governed asset truth. |
| **`preventive-maintenance-template`** | Stateless/entity / **E3** | **High-value future sample.** | Shows that rule-bearing templates can stand alone: interval basis, trigger mode, assignment scope, required approvals, shutdown-needed flag. |
| **`asset-criticality-policy`** | Stateless/entity / **E3** | **Optional later flagship.** | Strong domain-rule contract, but less urgent than failure codes and PM templates. |

---

## 4. Recommended sequencing relative to the current plan

### 4.1 Immediate PM change to the rewrite plan

**Move `maintenance-work-order` into Wave 1 rewrites.**

Recommended revised rewrite order:

1. `it-helpdesk-ticket`
2. `maintenance-work-order`
3. `loan-application`
4. `insurance-claim`
5. `subscription-cancellation-retention`
6. `travel-reimbursement`
7. `clinic-appointment-scheduling`
8. `library-book-checkout`

**Defer `utility-outage-report` out of the first rewrite batch.**

Why:

- `maintenance-work-order` is already a known realism foundation in George's audit.
- It advances a missing **W3** lane faster than `utility-outage-report`, which mainly deepens an already-recognizable operational lane.
- Shane's domain knowledge makes this a high-leverage rewrite now, not a speculative later addition.

### 4.2 Net-new workflow sequencing

In the current addition plan, **re-scope `field-service-dispatch` into `equipment-failure-response`** if the portfolio still needs a distinct **W4 containment/remediation** sample after the work-order rewrite.

That keeps maintenance coverage disciplined:

- **`maintenance-work-order`** owns **W3**
- **`equipment-failure-response`** owns **W4**
- no redundant generic dispatch sample sitting beside a maintenance workflow that already schedules technicians

### 4.3 Hybrid sequencing

Make **`asset-record-lifecycle` the first maintenance companion in Wave 3** and place it ahead of more generic hybrid ideas such as `vendor-onboarding-review`.

Reason:

- it gives the maintenance workflow a governed entity anchor
- it reinforces the entity-first directive
- it uses a domain with real enterprise field density instead of a lighter operational record

### 4.4 Stateless/entity sequencing once #22 is ready

Recommended order:

1. **`failure-code-catalog`** — first maintenance reference-data anchor
2. **`preventive-maintenance-template`** — first maintenance domain-rule/template anchor
3. **`asset-master-record`** — only if we want the zero-state form after proving the hybrid asset record

That sequence pairs well with the workflow/hybrid rollout:

- work order gives us the lifecycle
- asset record gives us the governed object
- failure codes and PM templates give us the serious maintenance reference model

---

## 5. Concrete sample bundle recommendation

### Bundle A — first maintenance bundle

| Order | Sample | Archetype / shape | Role in corpus |
|---|---|---|---|
| 1 | **`maintenance-work-order`** (rewrite) | Workflow / W3 | Flagship enterprise maintenance workflow |
| 2 | **`asset-record-lifecycle`** | Hybrid / H1 | Companion governed entity with light lifecycle |
| 3 | **`failure-code-catalog`** | Stateless/entity / E1 | Reference-data companion once #22 is ready |
| 4 | **`preventive-maintenance-template`** | Stateless/entity / E3 | Policy/template companion once #22 is ready |

### Why this is the right first bundle

- It starts with the most recognizable enterprise maintenance artifact: the **work order**.
- It immediately pairs lifecycle depth with a governed companion entity: the **asset record**.
- It reserves the most authentic maintenance reference-data pieces for the proper lane instead of forcing them into fake workflows.

---

## 6. PM call

EAM is not a side-domain add-on. It is one of the cleanest proofs that the current sample strategy is right: **workflow, hybrid, and entity samples belong together.** The practical move is to **upgrade `maintenance-work-order` into the flagship W3 maintenance sample, pair it with `asset-record-lifecycle`, and reserve failure codes plus PM templates for the first maintenance data-only wave.**
