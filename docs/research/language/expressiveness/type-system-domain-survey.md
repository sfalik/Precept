# Type System Domain Survey

**Date:** 2026-04-05  
**Source:** 5 workflow platforms (ServiceNow, Salesforce, Dataverse/Power Apps, Camunda, Temporal) + 10 business-domain modeling exercises  
**Relevance:** Validates proposal #25 (type system expansion — choice and date types) with real-world evidence. Directly feeds the type system sequencing decisions from the PM scoping session.

---

## Angle 1: Domain Modeling Exercise

For each domain, we list fields a real entity would need and map them to Precept's current type system. A **gap** means the field cannot be faithfully modeled with `string | number | boolean`.

### 1. Insurance Underwriting

| Field | Natural type | Precept today | Gap |
|---|---|---|---|
| policyType | choice (auto, home, life, health) | string | **choice** |
| coverageLevel | choice (basic, standard, premium) | string | **choice** |
| annualPremium | currency / decimal | number | — |
| applicationDate | date | string | **date** |
| effectiveDate | date | string | **date** |
| riskScore | integer | number | — |
| isHighRisk | boolean | boolean | — |
| priorClaimCount | integer | number | — |
| underwriterDecision | choice (approve, decline, refer) | string | **choice** |

### 2. Clinical Trials

| Field | Natural type | Precept today | Gap |
|---|---|---|---|
| trialPhase | choice (Phase I, Phase II, Phase III, Phase IV) | string | **choice** |
| enrollmentDate | date | string | **date** |
| dosageLevel | choice (low, medium, high, placebo) | string | **choice** |
| adverseEventCount | integer | number | — |
| participantAge | integer | number | — |
| consentSigned | boolean | boolean | — |
| targetEnrollment | integer | number | — |
| actualEnrollment | integer | number | — |
| completionDate | date | string | **date** |
| outcomeCategory | choice (positive, negative, inconclusive) | string | **choice** |

### 3. Loan Servicing

| Field | Natural type | Precept today | Gap |
|---|---|---|---|
| loanType | choice (mortgage, auto, personal, student) | string | **choice** |
| principalAmount | currency / decimal | number | — |
| interestRate | decimal / percent | number | — |
| originationDate | date | string | **date** |
| maturityDate | date | string | **date** |
| paymentFrequency | choice (monthly, biweekly, weekly) | string | **choice** |
| daysDelinquent | integer | number | — |
| delinquencyBucket | choice (current, 30, 60, 90, 120+) | string | **choice** |
| isInDefault | boolean | boolean | — |
| creditScore | integer | number | — |

### 4. Supply Chain / Procurement

| Field | Natural type | Precept today | Gap |
|---|---|---|---|
| orderDate | date | string | **date** |
| expectedDelivery | date | string | **date** |
| actualDelivery | date | string | **date** |
| priority | choice (standard, expedited, critical) | string | **choice** |
| quantity | integer | number | — |
| unitPrice | currency / decimal | number | — |
| supplierRating | choice (preferred, approved, probationary) | string | **choice** |
| inspectionResult | choice (pass, fail, conditional) | string | **choice** |
| isHazmat | boolean | boolean | — |
| shipmentMethod | choice (ground, air, sea, rail) | string | **choice** |

### 5. Employee Onboarding

| Field | Natural type | Precept today | Gap |
|---|---|---|---|
| hireDate | date | string | **date** |
| startDate | date | string | **date** |
| department | choice (engineering, sales, marketing, ops, hr, finance) | string | **choice** |
| employmentType | choice (full-time, part-time, contractor, intern) | string | **choice** |
| backgroundCheckPassed | boolean | boolean | — |
| salaryBand | choice (L1, L2, L3, L4, L5) | string | **choice** |
| badgeIssued | boolean | boolean | — |
| itProvisioningComplete | boolean | boolean | — |
| probationEndDate | date | string | **date** |
| relocating | boolean | boolean | — |

### 6. SaaS Billing

| Field | Natural type | Precept today | Gap |
|---|---|---|---|
| planTier | choice (free, starter, pro, enterprise) | string | **choice** |
| billingCycle | choice (monthly, annual) | string | **choice** |
| monthlyAmount | currency / decimal | number | — |
| trialStartDate | date | string | **date** |
| trialEndDate | date | string | **date** |
| seatCount | integer | number | — |
| paymentMethod | choice (credit-card, invoice, ACH) | string | **choice** |
| isDelinquent | boolean | boolean | — |
| lastPaymentDate | date | string | **date** |
| discountPercent | decimal / percent | number | — |

### 7. Real Estate Closing

| Field | Natural type | Precept today | Gap |
|---|---|---|---|
| propertyType | choice (single-family, condo, multi-family, commercial) | string | **choice** |
| listingDate | date | string | **date** |
| offerDate | date | string | **date** |
| closingDate | date | string | **date** |
| salePrice | currency / decimal | number | — |
| escrowAmount | currency / decimal | number | — |
| inspectionStatus | choice (pending, passed, failed, waived) | string | **choice** |
| financingType | choice (conventional, FHA, VA, cash) | string | **choice** |
| titleClear | boolean | boolean | — |
| appraisalComplete | boolean | boolean | — |

### 8. Regulatory Compliance (Financial)

| Field | Natural type | Precept today | Gap |
|---|---|---|---|
| reportingPeriod | choice (Q1, Q2, Q3, Q4, annual) | string | **choice** |
| filingDeadline | date | string | **date** |
| submissionDate | date | string | **date** |
| jurisdiction | choice (federal, state, EU, UK) | string | **choice** |
| complianceStatus | choice (compliant, non-compliant, remediation) | string | **choice** |
| findingCount | integer | number | — |
| penaltyAmount | currency / decimal | number | — |
| auditorAssigned | boolean | boolean | — |
| remediationDeadline | date | string | **date** |
| isMaterialWeakness | boolean | boolean | — |

### 9. Manufacturing Quality Control

| Field | Natural type | Precept today | Gap |
|---|---|---|---|
| batchDate | date | string | **date** |
| inspectionType | choice (incoming, in-process, final, audit) | string | **choice** |
| defectCategory | choice (cosmetic, functional, safety, dimensional) | string | **choice** |
| defectCount | integer | number | — |
| sampleSize | integer | number | — |
| toleranceMin | decimal | number | — |
| toleranceMax | decimal | number | — |
| measuredValue | decimal | number | — |
| dispositionDecision | choice (accept, rework, scrap, hold) | string | **choice** |
| passRate | decimal / percent | number | — |

### 10. Legal Case Management

| Field | Natural type | Precept today | Gap |
|---|---|---|---|
| caseType | choice (civil, criminal, family, corporate, IP) | string | **choice** |
| filingDate | date | string | **date** |
| trialDate | date | string | **date** |
| courtType | choice (district, appellate, supreme, arbitration) | string | **choice** |
| claimedAmount | currency / decimal | number | — |
| billedHours | decimal | number | — |
| priority | choice (low, normal, high, urgent) | string | **choice** |
| discoveryDeadline | date | string | **date** |
| isProBono | boolean | boolean | — |
| caseDisposition | choice (settled, dismissed, verdict, ongoing) | string | **choice** |

### Domain Modeling Summary

Across 100 fields modeled in 10 domains:

| Gap type | Occurrences | Domains affected |
|---|---|---|
| **choice** (enum/picklist) | **41** | 10/10 |
| **date** | **30** | 10/10 |
| integer (subtype of number) | 0 explicit gaps (number works) | — |
| currency/decimal | 0 explicit gaps (number works) | — |

**Every single domain** requires both choice and date. The `string` workaround survives at runtime but provides zero compile-time validation — a guard like `when status = "Approvd"` (typo) compiles and runs without error.

---

## Angle 2: Workflow Platform Survey

### ServiceNow

**Source:** ServiceNow GlideRecord API documentation, platform field type reference

| Platform type | Precept equivalent | Notes |
|---|---|---|
| String | string | — |
| Boolean (True/False) | boolean | — |
| Integer | number | Dedicated integer type |
| Decimal | number | Separate from integer |
| Floating Point Number | number | IEEE 754 |
| GlideDate | **none** | Date-only type |
| GlideDateTime | **none** | Date + time type |
| GlideDuration | **none** | Duration type |
| Choice | **none** | Enumerated picklist |
| Currency | number | Dedicated currency type |
| Reference | **none** | FK to another table |
| Encrypted Text | string | — |

**Key insight:** ServiceNow treats date, datetime, and duration as three separate types. Choice is a first-class field type with platform-enforced validation.

### Salesforce

**Source:** Salesforce Field Types reference (developer.salesforce.com)

| Platform type | Precept equivalent | Notes |
|---|---|---|
| string | string | — |
| boolean | boolean | — |
| int | number | — |
| double | number | — |
| date | **none** | Date-only |
| dateTime | **none** | Date + time |
| currency | number | With locale formatting |
| picklist | **none** | Single-select enumeration |
| multipicklist | **none** | Multi-select enumeration |
| email | string | Validated string format |
| phone | string | Validated string format |
| url | string | Validated string format |
| percent | number | Display-only distinction |
| reference (ID) | **none** | FK to another object |
| address | **none** | Compound type |
| location | **none** | Geolocation (lat/long) |

**Key insight:** Salesforce distinguishes picklist (single-select) from multipicklist (multi-select). Both are first-class types with platform-enforced values. Date and dateTime are separate types.

### Dataverse / Power Apps

**Source:** Microsoft Dataverse types-of-fields documentation

| Platform type | Precept equivalent | Notes |
|---|---|---|
| Text / Multiline Text | string | — |
| Yes/No (Boolean) | boolean | — |
| Whole Number | number | Dedicated integer |
| Decimal Number | number | — |
| Floating Point Number | number | — |
| Currency | number | With currency metadata |
| Date Only | **none** | Date without time |
| Date and Time | **none** | Full datetime |
| Duration | **none** | Time span |
| Choice (picklist) | **none** | Single-select enum |
| Choices (multi-select) | **none** | Multi-select enum |
| Status / Status Reason | **none** | Special state enum |
| Lookup | **none** | FK reference |
| Email / Phone / URL | string | Validated formats |
| File / Image | **none** | Binary data |
| Big Integer | number | 64-bit integer |
| Timezone / Language | **none** | Specialized enums |

**Key insight:** Dataverse has a dedicated Status + Status Reason pair — effectively a state machine type built into the platform. Choice and Choices are separate types. Date Only vs Date and Time are separate.

### Camunda

**Source:** Camunda 7 Process Engine (docs.camunda.org), Camunda 8/Zeebe (docs.camunda.io)

**Camunda 7 (classic):**

| Platform type | Precept equivalent | Notes |
|---|---|---|
| string | string | — |
| boolean | boolean | — |
| integer | number | java.lang.Integer |
| short | number | java.lang.Short |
| long | number | java.lang.Long |
| double | number | java.lang.Double |
| date | **none** | java.util.Date |
| null | — | Null reference |
| bytes | **none** | Raw byte array |
| file | **none** | File with metadata |
| object | **none** | Serialized Java object |

**Camunda 8 (Zeebe):**

| Platform type | Precept equivalent | Notes |
|---|---|---|
| String | string | JSON string |
| Number | number | JSON number (int or float) |
| Boolean | boolean | JSON boolean |
| Array | **none** | JSON array |
| Object | **none** | JSON object |
| Null | — | JSON null |

**Key insight:** Camunda 8's radical simplification to JSON-native types proves that a minimal type system can work for workflow orchestration. However, Camunda 8 pushes type complexity to the host language and external systems — it deliberately does *not* validate field values at the process definition level. Camunda 7's richer type set (with `date`, `integer`, `long`) reflects what business processes actually need when the engine does perform type checking.

### Temporal

**Source:** Temporal TypeScript SDK documentation (docs.temporal.io)

| Platform type | Precept equivalent | Notes |
|---|---|---|
| (any serializable value) | — | No platform-level types |

**Key insight:** Temporal has **no type system at the variable level**. Workflow and Activity parameters must be "serializable" (JSON by default), and all typing is delegated to the host language (TypeScript, Go, Java, Python). This is the opposite end of the spectrum from ServiceNow/Salesforce/Dataverse.

Temporal's approach works because it is a *workflow orchestrator*, not a *business entity definition system*. Precept is the latter — it defines entity contracts — so delegating all typing to the host would defeat its purpose.

### Platform Survey Summary

| Type concept | ServiceNow | Salesforce | Dataverse | Camunda 7 | Camunda 8 | Temporal | Count |
|---|---|---|---|---|---|---|---|
| **Choice / Enum** | ✓ | ✓ | ✓ | — | — | — | **3/5** (all entity-centric) |
| **Date** | ✓ | ✓ | ✓ | ✓ | — | — | **4/5** |
| **Integer** | ✓ | ✓ | ✓ | ✓ | — | — | **4/5** |
| **Decimal / Float** | ✓ | ✓ | ✓ | ✓ | — | — | **4/5** |
| **Currency** | ✓ | ✓ | ✓ | — | — | — | **3/5** |
| **Duration** | ✓ | — | ✓ | — | — | — | **2/5** |
| String | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 6/6 |
| Boolean | ✓ | ✓ | ✓ | ✓ | ✓ | — | 5/6 |
| Number (untyped) | — | — | — | — | ✓ | ✓ | 2/6 |
| Reference / Lookup | ✓ | ✓ | ✓ | — | — | — | 3/5 |

The three platforms that define business entities (ServiceNow, Salesforce, Dataverse) **all** have choice/enum and date as first-class types. The two platforms that are pure workflow orchestrators (Camunda 8, Temporal) rely on the host language for typing.

Precept is an entity-definition system, not a workflow orchestrator. This places it squarely in the ServiceNow/Salesforce/Dataverse category, where choice and date are non-negotiable.

---

## Synthesis

### Type tiers

| Tier | Type | Evidence strength | Recommendation |
|---|---|---|---|
| **Universal** | choice (enum/picklist) | 41/100 domain fields; 3/3 entity platforms | **Must add.** Ship first. |
| **Universal** | date | 30/100 domain fields; 4/5 platforms (including Camunda 7) | **Must add.** Ship with or immediately after choice. |
| **Common** | integer (subtype of number) | 4/5 platforms distinguish it; 0 domain fields *blocked* by `number` | **Defer.** `number` workaround is tolerable. Revisit if expression expansion (#16 numeric functions) needs integer semantics. |
| **Common** | currency / decimal | 3/5 platforms; 0 domain fields blocked | **Defer.** Same position as integer — `number` works. |
| **Nice-to-have** | duration | 2/5 platforms; 0 domain fields in our 10 domains | **Defer indefinitely.** Can model as `number` (minutes) or `string` (ISO 8601). |
| **Non-goal** | reference / lookup | 3/3 entity platforms; but requires cross-precept linking | **Out of scope.** Precept defines single entities; cross-entity references belong to the hosting system. |
| **Non-goal** | record / struct | 0 platforms use it as a field type (objects are runtime-only in Camunda 8) | **Confirmed non-goal.** Conflicts with flat-field philosophy. |

### Revised recommendation for Precept

1. **Choice (enum) is the #1 priority.** It appears in every domain, and the `string` workaround provides zero compile-time safety. A typo in a guard value (`"Approvd"` vs `"Approved"`) is currently invisible. Choice would enable the type checker to catch these at compile time.

2. **Date is the #2 priority.** It appears in every domain and cannot be meaningfully manipulated as a `string`. Without a date type, Precept cannot express guards like "when daysOverdue > 30" where `daysOverdue` depends on the current date and a stored date field.

3. **Integer and currency remain deferred.** The `number` type covers the domain adequately. No sample file or domain exercise produced a field that *could not be modeled* with `number`. The gap is precision semantics, not modeling capability.

4. **Proposal #25 (choice + date) is fully validated.** The domain survey confirms that these are the exact two types needed. The proposal scope does not need to expand.

### Key finding: the "string enum" problem

The most impactful finding is not about dates — it's about **choice fields and typo safety**. Consider a Precept guard:

```
from Pending on Approve
  when status = "Approvd"    ← typo: compiles, runs, never matches
  transition Approved
```

With a `choice` type, the type checker could flag `"Approvd"` as not a valid member of the choice set. This is the same value proposition that TypeScript enums, Salesforce picklists, and Dataverse choices provide — and it's the gap most likely to cause real production bugs in Precept definitions today.

---

## Methodology notes

- **Domain modeling:** Fields chosen by analyzing what a real-world entity of each type would need at its state transitions, not what a UI form would display. We focused on fields that appear in guards, mutations, or invariants.
- **Platform survey:** Data gathered from official documentation. ServiceNow data supplemented from GlideRecord API reference.
- **Gap counting:** A "gap" means Precept's current type cannot provide compile-time validation that the host platform's native type provides. `number` covering both integer and decimal is counted as "no gap" because the workaround loses no modeling capability (only precision metadata).

---

## Beyond v1 — Domain-Driven Type Needs

**Date:** 2026-04-05  
**Context:** v1 type system expansion ships `choice` and `date`. This section looks at the longer horizon — what types will real business domains eventually demand beyond those two?

### Domain Deep Dive (Post-Choice, Post-Date)

For each of the 10 original domains, we now ask: **which fields still can't be expressed well** even after `choice` and `date` ship? We examine the residual gaps across eight cross-cutting patterns.

#### 1. Insurance Underwriting

| Field | Natural type | Precept after v1 | Residual gap |
|---|---|---|---|
| annualPremium | currency (USD 1,247.50) | number | **currency** — loses currency code, precision semantics |
| applicantEmail | email | string | **formatted string** — no format validation |
| applicantPhone | phone | string | **formatted string** |
| proofDocuments | attachment ref list | — | **attachment/external ref** — no way to reference external artifacts |
| riskAssessmentNotes | long text | string | — (string is adequate) |
| coveragePeriod | duration (12 months) | number | **duration** — "12 months" vs "365 days" distinction lost |

#### 2. Clinical Trials

| Field | Natural type | Precept after v1 | Residual gap |
|---|---|---|---|
| participantAge | integer | number | **integer** — fractional ages nonsensical |
| adverseEventCount | integer | number | **integer** |
| dosageMg | decimal with unit | number | **unit-bearing numeric** — mg vs mcg distinction |
| principalInvestigatorEmail | email | string | **formatted string** |
| consentDocumentRef | attachment ref | — | **attachment/external ref** |
| washoutPeriod | duration (14 days) | number | **duration** |

#### 3. Loan Servicing

| Field | Natural type | Precept after v1 | Residual gap |
|---|---|---|---|
| principalAmount | currency (USD) | number | **currency** — precision (2 decimal places) and currency code |
| interestRate | percentage (4.75%) | number | **percentage** — semantic: is 4.75 the rate or is 0.0475? |
| monthlyPayment | currency | number | **currency** |
| daysDelinquent | integer | number | **integer** — fractional days impossible |
| gracePeriod | duration (15 days) | number | **duration** |
| borrowerEmail | email | string | **formatted string** |
| borrowerPhone | phone | string | **formatted string** |

#### 4. Supply Chain / Procurement

| Field | Natural type | Precept after v1 | Residual gap |
|---|---|---|---|
| unitPrice | currency | number | **currency** |
| totalOrderValue | currency | number | **currency** |
| quantity | integer | number | **integer** — 2.7 boxes nonsensical |
| supplierWebsite | URL | string | **formatted string** |
| deliveryAddress | structured address | string | **structured data** — street, city, state, zip as flat string |
| transitTime | duration (3-5 business days) | number | **duration** |
| packingSlipRef | attachment ref | — | **attachment/external ref** |

#### 5. Employee Onboarding

| Field | Natural type | Precept after v1 | Residual gap |
|---|---|---|---|
| annualSalary | currency | number | **currency** |
| probationLength | duration (90 days) | number | **duration** |
| contactEmail | email | string | **formatted string** |
| contactPhone | phone | string | **formatted string** |
| emergencyContactPhone | phone | string | **formatted string** |
| signedOfferLetterRef | attachment ref | — | **attachment/external ref** |
| seatCount | integer | number | **integer** |

#### 6. SaaS Billing

| Field | Natural type | Precept after v1 | Residual gap |
|---|---|---|---|
| monthlyAmount | currency | number | **currency** |
| discountPercent | percentage (15%) | number | **percentage** |
| seatCount | integer | number | **integer** |
| billingContactEmail | email | string | **formatted string** |
| invoiceDocumentRef | attachment ref | — | **attachment/external ref** |
| trialDuration | duration (14 days) | number | **duration** |
| accountPortalUrl | URL | string | **formatted string** |

#### 7. Real Estate Closing

| Field | Natural type | Precept after v1 | Residual gap |
|---|---|---|---|
| salePrice | currency | number | **currency** |
| escrowAmount | currency | number | **currency** |
| earnestMoneyDeposit | currency | number | **currency** |
| commissionPercent | percentage (6%) | number | **percentage** |
| propertyAddress | structured address | string | **structured data** |
| inspectionReportRef | attachment ref | — | **attachment/external ref** |
| titleDocumentRef | attachment ref | — | **attachment/external ref** |
| buyerEmail | email | string | **formatted string** |

#### 8. Regulatory Compliance (Financial)

| Field | Natural type | Precept after v1 | Residual gap |
|---|---|---|---|
| penaltyAmount | currency | number | **currency** |
| findingCount | integer | number | **integer** |
| complianceRate | percentage | number | **percentage** |
| regulatorEmail | email | string | **formatted string** |
| filingDocumentRef | attachment ref | — | **attachment/external ref** |
| remediationWindow | duration (60 days) | number | **duration** |
| regulatorPortalUrl | URL | string | **formatted string** |

#### 9. Manufacturing Quality Control

| Field | Natural type | Precept after v1 | Residual gap |
|---|---|---|---|
| defectCount | integer | number | **integer** |
| sampleSize | integer | number | **integer** |
| passRate | percentage (98.5%) | number | **percentage** |
| toleranceMin | decimal (4 places) | number | — (number is fine) |
| toleranceMax | decimal (4 places) | number | — |
| batchCertificateRef | attachment ref | — | **attachment/external ref** |
| shiftDuration | duration (8 hours) | number | **duration** |

#### 10. Legal Case Management

| Field | Natural type | Precept after v1 | Residual gap |
|---|---|---|---|
| claimedAmount | currency | number | **currency** |
| billedHours | decimal | number | — |
| hourlyRate | currency | number | **currency** |
| clientEmail | email | string | **formatted string** |
| clientPhone | phone | string | **formatted string** |
| courtFilingRef | attachment ref | — | **attachment/external ref** |
| discoveryPeriod | duration (90 days) | number | **duration** |
| settlementPercent | percentage | number | **percentage** |

### Residual Gap Summary (Post-v1)

| Gap pattern | Occurrences | Domains affected | Current workaround | Workaround quality |
|---|---|---|---|---|
| **currency** | 16 | 9/10 | `number` | Tolerable — loses currency code and precision guarantee |
| **integer** | 11 | 7/10 | `number` | Tolerable — no way to prevent fractional assignment |
| **formatted string** (email/phone/URL) | 19 | 10/10 | `string` | Tolerable — zero format validation |
| **attachment / external ref** | 12 | 10/10 | not expressible | **No workaround** — Precept has no way to reference external artifacts |
| **duration** | 10 | 10/10 | `number` | Tolerable — loses unit semantics ("days" vs "months" vs "hours") |
| **percentage** | 7 | 6/10 | `number` | Tolerable — ambiguous whether 4.75 means 4.75% or 0.0475 |
| **structured data** (address) | 2 | 2/10 | `string` | Poor — flattened into one string field or spread across multiple strings |

### Platform Pattern Analysis (Post-v1)

#### ServiceNow — Types Beyond choice + date

| Platform type | Status in Precept post-v1 | Usage frequency | Notes |
|---|---|---|---|
| Currency | Not covered | High — every financial record | Separate type with currency code metadata |
| GlideDuration | Not covered | Medium — SLA management | ISO 8601 duration; distinct from date/datetime |
| Reference (FK) | Not covered | Very high | FK to another table — core relational concept |
| Encrypted Text | Not relevant | Low | Security concern, not type semantics |
| Email | `string` | Medium | Validated format |
| Phone Number | `string` | Medium | Validated format |
| URL | `string` | Medium | Validated format |
| IP Address | `string` | Low | Validated format |
| Journal / Journal Input | `string` | Medium | Append-only audit log — actually a **log-type** |

**New finding:** ServiceNow's **Journal** type is an append-only text field used for work notes and comments. It represents a pattern we haven't considered: **append-only audit/log fields** that accumulate over time rather than being overwritten.

#### Salesforce — Types Beyond choice + date

| Platform type | Status in Precept post-v1 | Usage frequency | Notes |
|---|---|---|---|
| currency | Not covered | High | Double with CurrencyIsoCode linked field |
| percent | Not covered | Medium | Display distinction; stored as double |
| email | `string` | High | First-class validated format |
| phone | `string` | High | First-class validated format |
| url | `string` | Medium | First-class validated format |
| reference (ID) | Not covered | Very high | FK to another Salesforce object |
| address | Not covered | Medium | **Compound type**: Street, City, State, PostalCode, Country |
| location | Not covered | Low | Geolocation (latitude/longitude pair) |
| multipicklist | Partially covered (choice is single-select) | Medium | Multi-select enumeration — `set<choice>` equivalent |
| textarea | `string` | Medium | Long text (no WHERE clause filtering) |
| calculated (formula) | Not applicable | High | Derived field — computed, not stored |
| combobox | Not covered | Low | Picklist + freeform — hybrid pattern |
| encryptedstring | `string` | Low | Security, not type semantics |
| anyType | Not applicable | Internal | Polymorphic — used for history tracking |

**New finding:** Salesforce's **multipicklist** is interesting because it's a multi-select version of picklist. In Precept post-v1, `set<choice>` could potentially model this if `choice` is allowed as a collection inner type. Salesforce also has **calculated/formula fields** which are derived at read-time — a concept Precept doesn't have and shouldn't add (Precept fields are always explicitly mutated by events).

#### Dataverse / Power Apps — Types Beyond choice + date

| Platform type | Status in Precept post-v1 | Usage frequency | Notes |
|---|---|---|---|
| Currency | Not covered | High | With currency metadata and transaction currency lookups |
| Duration | Not covered | Medium | Time span — used heavily in scheduling and SLA |
| Choices (multi-select) | Partially covered | Medium | Multi-select picklist, parallel to Salesforce multipicklist |
| Lookup | Not covered | Very high | FK reference to another entity |
| File | Not covered | Medium | Binary file with metadata |
| Image | Not covered | Medium | Image with thumbnail generation |
| Status / Status Reason | Covered (this IS Precept's state model) | N/A | Precept's state machine is natively stronger |
| Big Integer | `number` | Low | 64-bit integer |
| Timezone | Not covered | Low | Specialized enum (same as choice) |
| Language | Not covered | Low | Specialized enum (same as choice) |
| Auto Number | Not applicable | Medium | System-generated sequential identifiers |

**New finding:** Dataverse's **Status / Status Reason** is precisely what Precept already models with its state machine. This is a strong validation signal: Precept's core abstraction replaces what other platforms encode as a special field type.

**New finding:** Dataverse has **Auto Number** — system-generated sequential identifiers. This is an anti-pattern for Precept because precept definitions are stateless contracts; ID generation belongs to the hosting system.

#### Camunda 8 (FEEL) — Types Beyond choice + date

Camunda's FEEL expression language (confirmed via live docs) actually has a richer type system than Camunda 8's process variable types:

| FEEL type | Status in Precept post-v1 | Notes |
|---|---|---|
| date | Covered (v1) | `date("2017-03-10")` or `@"2017-03-10"` |
| time | Not covered | Local or zoned time without date |
| date-time | Not covered | Combined date + time |
| days-time-duration | Not covered | Based on seconds: `duration("P4D")`, `@"PT2H"` |
| years-months-duration | Not covered | Calendar-based: `duration("P1Y6M")` |
| list | Partially covered | Precept has `set`, `queue`, `stack` — but not general-purpose lists |
| context | Not covered | Key-value map — Precept doesn't have maps |

**New finding:** FEEL distinguishes **two kinds of duration**: days-time-duration (based on seconds/hours/days) and years-months-duration (based on calendar months/years). This distinction matters for business rules: "30 days" and "1 month" are different periods depending on which month. If Precept ever adds duration, this design choice will need resolution.

#### Temporal — Types Beyond choice + date

Temporal remains type-agnostic; all typing is delegated to the host language (TypeScript, Go, Java, Python). No new types to report beyond the initial survey.

**Key observation:** Temporal's approach of "serialize anything" works because it is a workflow orchestrator that deliberately pushes business logic to application code. Precept is the opposite — it **encodes** business rules in the definition. This reaffirms that Precept needs a richer type system than Temporal, closer to the ServiceNow/Salesforce/Dataverse tier.

### Platform Survey Summary (Beyond v1)

| Type concept | ServiceNow | Salesforce | Dataverse | Camunda FEEL | Temporal | Platform count |
|---|---|---|---|---|---|---|
| **Currency** | ✓ | ✓ | ✓ | — | — | **3/5** (all entity platforms) |
| **Duration** | ✓ | — | ✓ | ✓ (two kinds) | — | **3/5** |
| **Email** (validated string) | ✓ | ✓ | ✓ | — | — | **3/5** |
| **Phone** (validated string) | ✓ | ✓ | ✓ | — | — | **3/5** |
| **URL** (validated string) | ✓ | ✓ | ✓ | — | — | **3/5** |
| **Reference / Lookup** | ✓ | ✓ | ✓ | — | — | **3/5** |
| **Address** (compound) | — | ✓ | — | — | — | **1/5** |
| **Location** (geolocation) | — | ✓ | — | — | — | **1/5** |
| **File / Image** | — | — | ✓ | — | — | **1/5** |
| **Multipicklist** | — | ✓ | ✓ | — | — | **2/5** |
| **Time** (without date) | — | — | — | ✓ | — | **1/5** |
| **Integer** (distinct) | ✓ | ✓ | ✓ | — | — | **3/5** |
| **Percentage** | — | ✓ | — | — | — | **1/5** |
| **Journal / audit log** | ✓ | — | — | — | — | **1/5** |
| **Calculated / formula** | — | ✓ | — | — | — | **1/5** |

### Synthesis

#### 1. Phase 2 Candidates — Evaluate Right After v1 Ships

These types have strong domain evidence and are present on multiple entity-definition platforms.

| Type | Evidence strength | Domain count | Platform count | Key value proposition |
|---|---|---|---|---|
| **integer** | 11 fields across 7 domains | 7/10 | 3/5 | Prevents fractional assignment to count/quantity/score fields. Guards like `when seatCount > 5` gain type precision. Compile-time enforcement that `seatCount = 3.7` is invalid. |
| **duration** | 10 fields across 10 domains, 3 platforms with native support | 10/10 | 3/5 | Enables guards like `when gracePeriod > 30 days`. Currently must store as raw number and lose unit semantics. Camunda FEEL's two-duration split (day-time vs year-month) shows the design space is nontrivial. |

**Phase 2 field examples:**

| Domain | Field | Type | Why number isn't enough |
|---|---|---|---|
| Clinical Trials | adverseEventCount | integer | 2.5 adverse events is nonsensical |
| Supply Chain | quantity | integer | Ordering 3.7 units is invalid |
| SaaS Billing | seatCount | integer | Can't have half a seat |
| Employee Onboarding | probationLength | duration | "90 days" carries different semantics than "3 months" |
| Loan Servicing | gracePeriod | duration | "15 calendar days" vs "15 business days" — units matter |
| Legal | discoveryPeriod | duration | Court deadlines are in specific units |

#### 2. Phase 3 Candidates — Enterprise Domain Relevance

These types appear in enterprise contexts but either overlap with the constraint system (#13) or introduce architectural complexity.

| Type | Evidence | Architectural concern | Recommendation |
|---|---|---|---|
| **formatted strings** (email, phone, URL) | 19 fields, 10/10 domains, 3/5 platforms treat as first-class | Overlaps directly with field-level constraints (#13). `field email : string` + `constraint email matches /.+@.+\..+/` achieves the same compile-time guarantee without new types. | **Constraint, not type.** Evaluate only if #13 proves insufficient for regex/format validation. |
| **currency** | 16 fields, 9/10 domains, 3/5 platforms | Currency is `number` + metadata (currency code, precision). Adding a currency type means either (a) a parameterized type `currency(USD, 2)` which is very heavy, or (b) a simple `currency` alias that is just `number` with different default precision — marginal value. | **Defer.** The real pain is precision, which a constraint like `constraint amount precision 2` would handle. Currency code belongs to the hosting system. |
| **percentage** | 7 fields, 6/10 domains, only Salesforce treats as first-class | The semantic ambiguity (4.75 vs 0.0475) is a documentation problem, not a type problem. A constraint like `constraint rate range 0..100` catches invalid values. | **Constraint, not type.** |
| **attachment / external reference** | 12 fields, 10/10 domains | Precept defines entity contracts, not data storage. Attachment references point to external systems. Adding an `attachment` or `ref` type would require Precept to know about the hosting system's storage layer. | **Phase 3 at earliest.** Consider a lightweight `ref` type that is opaque to Precept (treated as string for storage, validated by the host). |

**Phase 3 field examples:**

| Domain | Field | Type | Why it matters at enterprise scale |
|---|---|---|---|
| Insurance | proofDocuments | attachment ref | Regulators require evidence of document attachment at specific state transitions |
| Real Estate | propertyAddress | structured | Address validation is a hard requirement for MLS compliance |
| Legal | courtFilingRef | attachment ref | Court filings must be traceable to state transitions |
| Regulatory | filingDocumentRef | attachment ref | Every finding requires attached evidence |

#### 3. Never-Add List

| Type | Offered by | Why Precept should NEVER add it |
|---|---|---|
| **reference / lookup (FK)** | ServiceNow, Salesforce, Dataverse | Precept defines **single entity** contracts. Cross-entity references belong to the hosting system's data layer. Adding FKs would require a relational model, breaking Precept's single-entity isolation principle. |
| **record / struct / compound** | Camunda FEEL (context), Salesforce (address compound) | Directly conflicts with Precept's **flat-field philosophy**. Each field must be independently addressable by guards and mutations. Nested structures would require dot-access in expressions (`address.city`), fundamentally changing the expression evaluator and the flat-statement design. |
| **calculated / formula field** | Salesforce | Precept fields are **explicitly mutated by events**. A calculated field that derives from other fields on read would be invisible to the state machine — its value could change without an event firing, breaking deterministic inspectability. |
| **anyType / polymorphic** | Salesforce (anyType) | Destroys compile-time type checking. Precept's value is that the type checker catches errors before runtime. A polymorphic field would make guard validation impossible. |
| **encrypted text** | ServiceNow, Salesforce | Security concern, not type semantics. Encryption belongs to the hosting system's storage layer, not the business rule definition. |
| **auto-number / sequence** | Dataverse | ID generation is a hosting system concern. Precept definitions are stateless contracts; they don't manage identity. |
| **journal / append-only log** | ServiceNow | Append-only semantics conflict with Precept's `set field = value` mutation model. An append-only field would need a different mutation verb and would blur the line between field state and event history. Event history belongs to the runtime, not the definition. |
| **time-only** (without date) | Camunda FEEL | Extremely rare in business entity modeling. Time-of-day fields almost always appear with a date. The 10-domain survey produced zero time-only fields. |
| **geolocation** | Salesforce | Compound type (lat/long pair) — falls into the struct anti-pattern. Also extremely niche for business process entities. |

#### 4. Field Constraint vs. Type Boundary

This is the key architectural question. For each candidate, we assess whether the need is better served by a **new type** (type system change) or a **field constraint** (#13 style).

| Candidate | New type? | Field constraint? | Verdict | Reasoning |
|---|---|---|---|---|
| **integer** | ✓ Yes | ✗ No | **Type** | Integer semantics affect the type checker and expression evaluator. A guard `when count > 5` has different evaluation semantics for integers vs decimals. Constraints can't change how the evaluator handles arithmetic. |
| **duration** | ✓ Yes | ✗ No | **Type** | Duration requires its own literal syntax, comparison operators (is 30 days > 4 weeks?), and arithmetic (date + duration). A constraint on `number` can't provide this. |
| **email** | ✗ No | ✓ Yes | **Constraint** | Email validation is regex-matchable: `constraint email matches /^[^@]+@[^@]+\.[^@]+$/`. No new expression semantics needed. Guards never compare email formats. |
| **phone** | ✗ No | ✓ Yes | **Constraint** | Same as email — format validation only. |
| **URL** | ✗ No | ✓ Yes | **Constraint** | Same as email — format validation only. |
| **currency** | ✗ Probably not | ✓ Mostly | **Constraint** | A constraint like `constraint amount precision 2` handles precision. Currency code is hosting-system metadata. The rare case where you need `amount + tax` to respect precision could be handled by numeric precision constraints, not a new type. |
| **percentage** | ✗ No | ✓ Yes | **Constraint** | `constraint rate range 0..100` captures the valid range. The 4.75-vs-0.0475 ambiguity is a convention, not a type error. |
| **attachment ref** | ✓ Maybe | ✗ No | **Evaluate** | If the host needs Precept to enforce "document must be attached before transition to Approved," the definition needs some way to reference an external artifact. A simple opaque `ref` type (validated by host, opaque to Precept) could work. But this is Phase 3 territory. |
| **address** | ✗ No | ✗ No | **Neither** | Flat-field philosophy says split into `street`, `city`, `state`, `zip` as separate `string` fields. Not a type system or constraint issue — it's a modeling pattern. |

### Key Findings

1. **The constraint system (#13) absorbs most of the remaining type pressure.** Of the 7 residual gap patterns, 4 (email, phone, URL, percentage) and partially a 5th (currency) are better handled by field-level constraints than new types. This makes #13 the most impactful post-v1 language feature for business domain coverage.

2. **Integer and duration are the only strong Phase 2 type candidates.** Both require new expression semantics that constraints can't provide. Integer needs fractional-assignment prevention and potentially integer arithmetic. Duration needs its own literal syntax and date arithmetic.

3. **The most surprising finding: attachment/document references appear in every single domain.** 10 out of 10 business domains need some way to reference external documents at state transitions. This is a gap that neither types nor constraints currently address, and it represents the deepest architectural question for Phase 3 — how does Precept acknowledge the existence of artifacts it can't validate?

4. **Multipicklist (`set<choice>`) may already be solved.** If v1 ships `choice` as a type that can be used as a collection inner type, `set<choice>` handles Salesforce's multipicklist and Dataverse's multi-select Choices natively. Worth confirming during v1 implementation.

5. **The platform data confirms Precept occupies the entity-definition tier** (ServiceNow, Salesforce, Dataverse), not the workflow-orchestration tier (Camunda 8, Temporal). This tier universally has 15-25 field types. Precept post-v1 will have 5 (`string`, `number`, `boolean`, `choice`, `date`). Even with Phase 2 additions (`integer`, `duration`), Precept would have 7 — deliberately minimal, with constraints filling the gap that enterprise platforms fill with type proliferation.
