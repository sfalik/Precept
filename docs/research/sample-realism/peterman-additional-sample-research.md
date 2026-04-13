# Additional External Research for Realistic Sample Authoring

**Author:** J. Peterman  
**Date:** 2026-04-08  
**Status:** Research artifact — completes the remaining external-facing lanes from `frank-enterprise-platform-and-research-gaps.md`  
**Depends on:** `README.md`, `peterman-enterprise-ecosystem-benchmarks.md`, `peterman-entity-centric-benchmarks.md`, `frank-enterprise-platform-and-research-gaps.md`  
**Philosophy flag:** Yes — this pass reinforces the directive that Precept is not workflow-only; it should model governed entities, policy-bearing contracts, and realistic institutional pressure even when today's DSL needs `FUTURE(...)` comments to carry the full domain truth.

---

## Purpose

Shane asked for the remaining outside research that would most directly strengthen realistic sample authoring. Frank's gap list had already identified the three highest-value lanes still open:

1. **Public process library mining** — real public case/process examples, not just platform marketing.
2. **Regulatory/compliance mining** — deadlines, evidence capture, appeals, remediation, auditability.
3. **Entity/reference-data corpora and schema ecosystems** — serious external anchors for governed entity and data-contract samples.

This document closes those three gaps and translates them into concrete sample-lane pressure.

---

## 1. Questions answered

1. What do public process corpora actually contain when you leave vendor tutorial land and look at real logs and public procedural systems?
2. Which public regulations most clearly force the kinds of deadlines, review history, evidence capture, authorization, and remediation logic that make samples feel institutional rather than toy-like?
3. Which public standards and schema ecosystems give us credible nouns, fields, identifiers, code lists, and versioning patterns for future governed-entity and reference-data samples?

---

## 2. Public process library mining

### 2.1 What the public corpora actually contain

| Source | Raw observation | Why it matters for Precept samples |
|---|---|---|
| [ProcessMining.org event-data index](https://www.processmining.org/event-data.html) | The public event-log shelf explicitly includes **loan application**, **road traffic fine management**, **purchase order handling**, **payment process of Common Agricultural Policy**, **incident management**, **problem management**, **complaints filed by customers**, **hospital billing**, and **sepsis cases**. | The open corpus is not dominated by cheerful approval ladders. It is full of disputes, public administration, healthcare, service operations, procurement, and payment enforcement. That is where realistic sample pressure lives. |
| [BPI Challenge 2017 loan application log](https://doi.org/10.4121/uuid:5f3067df-f10b-45da-b98b-86ae4c7a310b) | The log covers **all applications filed through an online system in 2016** and notes that the newer system allows **multiple offers per application**, with offers tracked by their own IDs. | A believable loan or underwriting sample should include amendment/counter-offer pressure. Real cases do not move through one clean approve/deny fork. |
| [Sepsis Cases event log](https://data.4tu.nl/datasets/33632f3c-5c48-40cf-8d8f-2db57f5a6ce7) | This real hospital log contains **about 1,000 cases**, **15,000 events**, **16 activities**, and **39 data attributes**, including responsible group, test results, and checklist information. | Healthcare realism comes from evidence fields, responsible-party metadata, and dense factual context around the lifecycle — not just stage names. |
| [IEEE Task Force / XES real-world logs](https://www.tf-pm.org/resources/xes-standard/about-xes/event-logs) and [ProcessMining.org event-data index](https://www.processmining.org/event-data.html) | The same public benchmark shelf keeps returning to **incidents**, **problems**, **complaints**, **fines**, **billing**, **loan applications**, and **procurement/payments**. | Future flagship samples should bias toward governed case files under operational or regulatory pressure, not abstract task choreography. |

### 2.2 What this means

Three public-process signals stood out:

1. **Real public corpora are exception-heavy.** The public benchmark world keeps publishing complaints, fines, incidents, and applications with rework — not just happy-path fulfillment.
2. **Serious cases carry dense metadata.** The sepsis log shows how real processes accumulate tests, checklist results, responsible groups, and timestamps around the same entity.
3. **Negotiation and reconsideration are normal.** The loan log's multiple-offer structure is exactly the kind of institutional realism that simple accept/reject demos erase.

### 2.3 Implication for sample authoring

The next sample wave should deliberately include **appeal**, **counter-offer**, **remediation**, **reopen**, or **collection/disposition** pressure. If a candidate sample cannot naturally host one of those shapes, it is probably not carrying enough real-world weight to be a flagship.

---

## 3. Regulatory and compliance requirement mining

### 3.1 High-value public obligations

| Source | Raw observation | Sample pressure it creates |
|---|---|---|
| [CMS Medicare Managed Care Appeals & Grievances](https://www.cms.gov/medicare/appeals-grievances/managed-care) | CMS updated Parts C and D guidance so the timeframe to submit an appeal moved from **60** to **65 calendar days from the date of the notice**, effective **01/01/2025**. | Appeal samples should have explicit filing clocks and date-driven eligibility, not a vague `Appealed` state. |
| [42 CFR 438.416](https://www.law.cornell.edu/cfr/text/42/438.416) | Every grievance/appeal record must capture a **general description of the reason**, **date received**, **date of each review**, **resolution at each level**, **date of resolution at each level**, and the **covered person**, and it must remain accessible to the state and available to CMS on request. | Appeals realism is not just a loop. It is **level-by-level review history plus evidence of who, when, and why**. |
| [CFPB Regulation E §1005.11](https://www.consumerfinance.gov/rules-policy/regulations/1005/11/) | Consumers generally must notify the institution within **60 days** of the statement; the institution must investigate within **10 business days**; it may take up to **45 days** if it provisionally credits the account within **10 business days**. | Financial dispute samples need clocks, provisional remedies, and a distinction between temporary and final outcomes. |
| [CISA BOD 22-01](https://www.cisa.gov/news-events/directives/bod-22-01-reducing-significant-risk-known-exploited-vulnerabilities) and [KEV Catalog](https://www.cisa.gov/known-exploited-vulnerabilities-catalog) | CISA maintains a catalog of known exploited vulnerabilities and requires remediation of catalog items; if an affected asset cannot be updated within the required timeframe, the asset must be removed from the agency network. The catalog exposes **Date Added**, **Due Date**, **Action**, and provides **CSV**, **JSON**, and **JSON Schema** feeds. | Security/remediation samples should model explicit due dates, action obligations, and an enforcement path when remediation is missed or impossible. |
| [HHS HIPAA Security Rule summary](https://www.hhs.gov/hipaa/for-professionals/security/laws-regulations/index.html), [45 CFR 164.312](https://www.ecfr.gov/current/title-45/subtitle-A/subchapter-C/part-164/subpart-C/section-164.312), [45 CFR 164.316](https://www.ecfr.gov/current/title-45/subtitle-A/subchapter-C/part-164) | HIPAA's Security Rule requires reasonable and appropriate safeguards for ePHI, including audit-control obligations under the technical safeguards, and the related compliance documentation is retained for years, not moments. | Healthcare and privacy samples should include audit-trail thinking and documentary permanence. The system is expected to remember. |

### 3.2 The pattern beneath the rules

Across those sources, the same five realism signals recur:

1. **Clocks matter.** Deadlines are first-class business facts.
2. **Evidence is part of the contract.** Review dates, reasons, notes, and supporting documentation are not side chatter.
3. **Intermediate outcomes are real outcomes.** Provisional credit, pending review, additional information, and containment are not implementation noise.
4. **Authorization is usually explicit.** A regulated process nearly always has a who-approved-it moment.
5. **Auditability is structural.** The record must survive review, not merely drive the next transition.

### 3.3 Implication for sample authoring

Any future compliance-facing sample should carry at least three of these five elements: **deadline field**, **evidence/history fields**, **authorization checkpoint**, **remediation obligation**, **review-level traceability**. Without them, it will read like a dramatization instead of a case file.

---

## 4. Entity, reference-data, and schema ecosystems

### 4.1 Serious external anchors for governed entities

| Source | Raw observation | What it gives Precept |
|---|---|---|
| [HL7 FHIR Organization](https://build.fhir.org/organization.html) | FHIR's `Organization` resource covers companies, institutions, departments, community groups, healthcare practice groups, and payer/insurer entities. It explicitly supports hierarchy via `partOf`, and the resource rule says the organization must have **at least a name or an identifier**. | A credible healthcare/entity sample should use real organizational nouns, identifiers, hierarchy, and role-bearing organization references instead of anonymous blobs. |
| [Open Contracting Data Standard schema reference](https://standard.open-contracting.org/latest/en/schema/reference/) | OCDS treats a contracting process as an append-only stream of **immutable releases**. The structure includes **parties**, **planning**, **tender**, **awards**, **contract**, and **implementation**, with closed codelists like `releaseTag` and `initiationType`. | Procurement realism wants party roles, versioned event history, and a distinction between a new release and an edited past. This is gold for governed-entity and lifecycle pairings. |
| [NIEM concepts reference](https://niem.github.io/reference/concepts/) | NIEM is built from authoritative **namespaces**, **properties**, **types**, **code sets/facets**, **roles**, **associations**, **references**, and **augmentation points**. | Public-sector or justice-adjacent samples should look like governed information exchanges with identifiers, roles, code constraints, and extension points — not flat records. |
| [OASIS UBL 2.4 OrderResponse summary](https://docs.oasis-open.org/ubl/os-UBL-2.4/mod/summary/reports/UBL-OrderResponse-2.4.html) | `OrderResponse` is explicitly a document used to indicate **detailed acceptance**, **rejection**, or **counter-offer** of an order. The model carries fields like `UBLVersionID`, `CustomizationID`, `ProfileID`, `ProfileExecutionID`, `ID`, and `SalesOrderID`. | Procurement samples should not stop at “approved.” Real order response ecosystems include counter-offers, collaboration identifiers, and profile metadata. |
| [ISO 20022 external code sets](https://www.iso20022.org/catalogue-messages/additional-content-messages/external-code-sets) | ISO 20022 external code sets are updated on a **quarterly cycle**, must be used in their **latest published version**, and are distributed in **XLSX**, **XSD**, and **JSON**. Change requests are a formal part of the operating model. | Reference data is versioned, governed, and publishable. A serious reference-data sample should have version semantics and validity expectations, not just a static list. |
| [Schema.org home](https://schema.org/) | Schema.org now spans **entities, relationships, and actions** across more than **45 million domains** and **450 billion objects**. | If we need a broad public vocabulary baseline for entity definitions, there is precedent for rich shared vocabularies that are extensible without becoming domain-empty. |
| [CISA KEV Catalog](https://www.cisa.gov/known-exploited-vulnerabilities-catalog) | The KEV catalog is not only a regulatory feed; it is also a live reference-data corpus with machine-readable structure, due dates, actions, notes, and product/vendor naming. | Precept can plausibly model not only remediation workflows but also the governed reference catalog that drives them. |

### 4.2 What the standards all agree on

Across healthcare, procurement, finance, public-sector exchange, and open vocabularies, the same entity signals recur:

1. **Identifiers are sacred.**
2. **Roles are explicit.**
3. **Code lists are governed separately from case flow.**
4. **Versioning matters.**
5. **History is publishable data, not a hidden implementation concern.**

### 4.3 Implication for sample authoring

Future entity and hybrid samples should borrow **real field families** from these ecosystems: identifier, status, type/category code, effective dates, party role, evidence/reference number, governing profile/version, and reason/status text. That is how the samples start sounding like software that belongs in an institution.

---

## 5. Concrete implications for future sample lanes

### 5.1 Recommended next lanes

| Lane | Best external anchors | Why it is strong | Companion entity/reference sample |
|---|---|---|---|
| **Benefits or prior-authorization appeal** | CMS appeals guidance; 42 CFR 438.416; FHIR Organization | Gives us deadlines, level-by-level review history, evidence capture, and clear institution roles. | **Payer / Provider Organization** registry with identifiers, hierarchy, and organization type constraints. |
| **Security vulnerability remediation case** | CISA BOD 22-01; KEV Catalog | A modern, AI-first, deadline-driven governed case with explicit due dates, actions, exceptions, and forced containment/removal paths. | **KEV catalog entry** or **software asset classification** contract with vendor/product/CVE/due-date/reference fields. |
| **Traffic fine / administrative penalty dispute** | Public process-mining corpora (`Road Traffic Fine Management Process`); NIEM | A public-law case shape with issuance, dispute, review, payment, and collection pressure. | **Party / citation / agency reference** contract using identifier, role, and code-list semantics. |
| **Purchase order exception / order response** | BPI procurement logs; OCDS; UBL OrderResponse | Lets Precept show acceptance, rejection, counter-offer, identifiers, and append-only release thinking. | **Vendor / contracting party** master-data contract with identifier, payment terms, bank/reference fields, and active/inactive governance. |
| **Clinical review / hospital escalation** | Sepsis event log; HIPAA security guidance; FHIR | Evidence-rich healthcare work with responsible groups, test results, and audit pressure. | **Clinical organization or care-team entity** with identifiers, hierarchy, and role-bearing references. |

### 5.2 Portfolio-level conclusion

The strongest next move is not “one more workflow sample.” It is **paired domain bundles**:

- one **governed case/lifecycle** sample, and
- one **companion governed entity or reference-data** sample from the same ecosystem.

Examples:

- **Prior authorization appeal** + **payer/provider organization registry**
- **KEV remediation case** + **KEV catalog entry**
- **Purchase order response** + **vendor master**
- **Traffic fine dispute** + **citation/party reference contract**

That pairing reflects the real philosophy now on the table: **Precept governs both motion and meaning.**

### 5.3 Authoring rule that follows from this research

Do not simplify the domain because the current DSL is still growing. If the real process has due dates, evidence packages, counter-offers, role-bearing identifiers, or review levels, keep them in the sample model and use `FUTURE(...)` comments where today's surface is thin. The public sources above justify that realism.

---

## 6. Conclusions

1. Public process corpora validate the team's instinct: serious samples should look like **case files under institutional pressure**, not generic approvals.
2. Regulatory sources show that realism is carried by **clocks, evidence, authorization, remediation, and audit history**.
3. Standards ecosystems show that serious entity samples need **identifiers, roles, code lists, versioning, and publishable history**.
4. The best future sample program is therefore **workflow + entity/reference-data bundles**, not workflow-only growth.

---

## 7. Source index

### Public process libraries and corpora

- [ProcessMining.org — Event Data](https://www.processmining.org/event-data.html)
- [IEEE Task Force on Process Mining — Real-world XES event logs](https://www.tf-pm.org/resources/xes-standard/about-xes/event-logs)
- [BPI Challenge 2017 — Loan application process of a Dutch financial institute](https://doi.org/10.4121/uuid:5f3067df-f10b-45da-b98b-86ae4c7a310b)
- [Sepsis Cases — Event Log](https://data.4tu.nl/datasets/33632f3c-5c48-40cf-8d8f-2db57f5a6ce7)
- [Road Traffic Fine Management Process](https://data.4tu.nl/articles/dataset/Road_Traffic_Fine_Management_Process/12683249)

### Regulatory and compliance sources

- [CMS — Medicare Managed Care Appeals & Grievances](https://www.cms.gov/medicare/appeals-grievances/managed-care)
- [42 CFR 438.416 — Recordkeeping requirements](https://www.law.cornell.edu/cfr/text/42/438.416)
- [CFPB Regulation E §1005.11](https://www.consumerfinance.gov/rules-policy/regulations/1005/11/)
- [CISA BOD 22-01](https://www.cisa.gov/news-events/directives/bod-22-01-reducing-significant-risk-known-exploited-vulnerabilities)
- [CISA Known Exploited Vulnerabilities Catalog](https://www.cisa.gov/known-exploited-vulnerabilities-catalog)
- [HHS — Summary of the HIPAA Security Rule](https://www.hhs.gov/hipaa/for-professionals/security/laws-regulations/index.html)
- [45 CFR 164.312 — Technical safeguards](https://www.ecfr.gov/current/title-45/subtitle-A/subchapter-C/part-164/subpart-C/section-164.312)
- [45 CFR Part 164 — Security and Privacy](https://www.ecfr.gov/current/title-45/subtitle-A/subchapter-C/part-164)

### Entity, reference-data, and schema ecosystems

- [HL7 FHIR — Organization](https://build.fhir.org/organization.html)
- [Open Contracting Data Standard — Schema reference](https://standard.open-contracting.org/latest/en/schema/reference/)
- [NIEM — Concepts reference](https://niem.github.io/reference/concepts/)
- [OASIS UBL 2.4 — OrderResponse summary](https://docs.oasis-open.org/ubl/os-UBL-2.4/mod/summary/reports/UBL-OrderResponse-2.4.html)
- [ISO 20022 — External code sets](https://www.iso20022.org/catalogue-messages/additional-content-messages/external-code-sets)
- [Schema.org](https://schema.org/)
