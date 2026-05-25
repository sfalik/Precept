# String Ordering Broad Use Cases: Cross-Domain Survey

> **Research question:** What genuine business use cases exist for string ordering (`<`, `>`, `<=`, `>=` on text fields) in business rules, workflows, and automation systems — across all business domains, not just healthcare?
>
> **String ordering defined here:** A business rule that says something like "if the customer name comes before 'M' alphabetically, route to team 1" or "if the serial number is between SN-10000 and SN-19999, flag for recall" or "if the account code starts before 'G' in the alphabet, apply rate tier A." This is ordinal (lexicographic) comparison of text values at the business-rule authoring level.

**Date:** 2026-05-24

**Prior related research:** This survey is intentionally independent of prior research files but is informed by their existence. For context, the prior files are:
- [`business-string-ordering-use-cases.md`](./business-string-ordering-use-cases.md) — surveyed case-insensitive ordering specifically
- [`string-ordering-external-survey.md`](./string-ordering-external-survey.md) — surveyed healthcare-specific code fields (ICD-10, ZIP, CPT)
- [`string-ordering-architectural-analysis.md`](./string-ordering-architectural-analysis.md) — architectural analysis
- [`string-ordering-gap-analysis.md`](./string-ordering-gap-analysis.md) — gap analysis between ordered choice and raw string ordering

---

## Background

Precept is a business-rule DSL for domain experts (not developers). The language supports string fields. A prior architectural analysis concluded against adding `<`/`>`/`<=`/`>=` for arbitrary strings, citing the case-insensitive ambiguity problem and the adequacy of ordered-choice types. However, this was primarily analyzed through the lens of healthcare code fields and the case-sensitivity issue.

This survey broadens the aperture: does string ordering appear as a first-class operator at the business-rule authoring level — in CRM platforms, ERP systems, no-code automation tools, manufacturing traceability, insurance underwriting, retail shipping, marketing automation, and other general business domains? The goal is to understand whether the practical demand signal is present outside healthcare, and whether platforms that domain experts actually use expose string `<`/`>` as a condition operator.

---

## Methodology

Web searches across eight primary categories using targeted queries for each platform type. For each category, the investigation sought:
1. Official documentation confirming or denying string `<`/`>` operator support
2. Community forum posts showing users requesting or workarounding this feature
3. Real practitioner examples from public sources
4. The dominant alternative approach when string ordering is unavailable

Approximately 25 separate web searches were conducted. Where official documentation was returned, that is treated as primary evidence. Community forum posts and blog posts are secondary evidence. Absence of documented examples after targeted search is noted as a finding, not assumed to be absence of practice.

---

## Findings by Category

### 1. Alphabetical Routing and Partitioning (CRM, Service Desks, Call Centers)

**Result: No evidence of string `<`/`>` as a first-class routing condition. Dominant approach is named ranges or round-robin, not direct alphabetical comparison operators.**

#### What platforms support

**Zendesk** trigger conditions reference confirmed: text field conditions support `is`, `contains`, `starts with`, `ends with` — not `less than` or `greater than`. The documented `less than` operator in Zendesk applies to **status hierarchy** (New < Open < Pending < Solved) — not to string fields. No evidence of users requesting or implementing alphabetical routing via string ordering in Zendesk triggers.
- Source: https://support.zendesk.com/hc/en-us/articles/4408893545882-Ticket-trigger-conditions-and-actions-reference

**Salesforce workflow rules:** Salesforce supports a `less than or equal to` operator in workflow rule criteria, but available evidence suggests this applies to numeric and date fields, not arbitrary text ordering. Text-field operators are `equals`, `starts with`, `contains`. The `BEGINS()` and `CONTAINS()` formula functions provide text matching. No evidence that Salesforce workflow rules support `account_name < "M"` as a routing condition.
- Source: https://help.salesforce.com/s/articleView?id=sf.workflow_rules_define.htm

**Microsoft Dynamics 365 / D365 Customer Service:** Unified Routing supports conditions on customer name attributes (first name, last name) but the documented operators for text fields are `equals`, `contains`, `starts with` — not `<`/`>`. One search result noted that round-robin and capacity-based assignment are the dominant automated routing mechanisms. Custom assignment methods (scripted) are available for complex cases.
- Source: https://learn.microsoft.com/en-us/dynamics365/customer-service/administer/assignment-methods

**ServiceNow business rules:** Text field string comparison in ServiceNow uses `contains` and `startsWith()` in GlideRecord queries. No evidence of `<` on string fields as a user-facing condition option in the GUI condition builder.
- Source: https://snprotips.com/blog/sncprotips/2015/12/undocumented-apis-glidefilterhtml

**HubSpot workflows:** HubSpot's native workflow conditions do not support property-to-property comparison of any kind as of the search results. A third-party app ("Custome Workflow Starter") provides string comparison as a custom workflow action, but the HubSpot community discussion reveals the request to compare string properties is long-standing and unmet natively. The documented comparison modes for text properties are `is`, `contains`, `does not contain`, `is not`, `is unknown`.
- Source: https://community.hubspot.com/t5/CRM/Is-there-a-method-to-Compare-user-properties-using-workflows-and/m-p/397158

**Call centers:** Modern call center routing uses round-robin, skills-based, least-idle, and capacity-based routing — not alphabetical partitioning. No evidence of alphabetical-range routing in mainstream workforce management systems.
- Source: https://callhippo.com/blog/general/skills-based-routing

#### What practitioners actually do for alphabetical workload splitting

When alphabetical partitioning is needed (e.g., "agents A-M handle customers with last names A-M"), practitioners implement this via:
1. Named agent pools / queues with explicit assignment lists
2. Custom code using `>= 'A' && < 'N'` in back-end filter logic (developer layer)
3. Dropdown / picklist fields that explicitly classify customers into buckets

**Assessment:** There is *folk knowledge* that alphabetical partitioning exists as a practice (it is mentioned as one of many workload-distribution approaches in lead routing articles). But the platforms that domain experts use to configure routing — Zendesk, Salesforce, HubSpot, D365, ServiceNow — do not expose string `<`/`>` as a GUI condition. The demand for this is low enough that none of the major CRM/helpdesk platforms have built it as a native condition operator.

**Evidence strength: Weak demand signal.** Acknowledged as a conceptual pattern but absent from the user-facing tooling of every major platform surveyed.

---

### 2. Serial Number / Lot Number Ranges in Manufacturing and Recall Management

**Result: Serial number ranges appear prominently in recall notices and ERP traceability queries, but the interaction model is range specification (from/to), not operator-based rule authoring.**

#### Oracle Supply Chain Management (recall management)

Oracle's Product Recall Management documentation explicitly states:

> "Recalls sometimes apply to specific ranges of serial numbers, or to serial numbers with specific prefixes or suffixes."

The system provides structured recall notice input fields where a starting serial number and ending serial number can be specified. The API exposes `recalllines` → `recalllotnumbers` → `recalllotserialnumbers` — a structured data model, not a rule expression language.
- Source: https://docs.oracle.com/en/cloud/saas/supply-chain-and-manufacturing/25b/fampr/locate-recalled-parts.html

#### SAP serial number management

SAP supports entering serial number ranges in the RF/warehouse management environment: "you can specify whole serial number ranges instead of individual serial numbers." This is for putaway/picking workflows — again a data input model, not a rule expression.
- Source: https://community.sap.com/t5/enterprise-resource-planning-q-a/reset-number-range-of-serial-number-management-and-auto-trigger-of-final/qaq-p/12643783

#### FDA / DSCSA pharmaceutical serialization

DSCSA (Drug Supply Chain Security Act) traceability is at the package-level serial number, lot number, and expiration date. Recall identification includes lot or serial number as key data elements. The FDA's regulatory framework describes recalls covering "specific lot or serial number ranges" but the implementation is via transaction data queries, not rule expressions.
- Sources: https://www.fda.gov/drugs/drug-supply-chain-security-act-dscsa, https://www.tracktracerx.com/7-important-questions-answered-about-the-dscsa-and-product-identifiers/

#### FSMA 204 food traceability

FDA's FSMA traceability rule uses Traceability Lot Codes (TLCs) as the primary identifier. In recall scenarios, the FDA specifies "foods and date ranges or traceability lot codes for which records must be provided." This is a query specification (which lot codes are involved), not a business rule expressing `serialNumber >= "SN-10000"`.
- Source: https://www.fda.gov/food/food-safety-modernization-act-fsma/fsma-final-rule-requirements-additional-traceability-records-certain-foods

#### Manufacturing ERP traceability

MES/ERP traceability systems support "forward trace from raw material to end customer" and "backward trace from finished unit to input materials." The recall question is answered by querying against stored lot/serial records, not by evaluating rules that express serial number range conditions.
- Source: https://www.symestic.com/en-us/what-is/batch-number

**Assessment:** Serial number ranges are a genuine and well-documented business concept in recall management. Oracle and SAP ERP systems model these as structured recall notice data (from/to fields in a recall record), not as business rule conditions. No evidence of domain experts authoring rules like `serialNumber >= "SN-10000" and serialNumber <= "SN-19999"` in a rule expression language. The comparison model is "this recall affects units whose serial numbers were input into the recall notice," not a continuously-evaluated predicate.

**Distinction from Precept's model:** A Precept precept governs how an *entity's data evolves under rules*. Recall queries are ad-hoc lookups against historical transaction data, not integrity rules that apply to an entity throughout its lifecycle. The use case is real but architectural.

**Evidence strength: Moderate demand signal for the concept, but the implementation pattern is structured recall notice data, not rule-expression string ordering. No evidence of rule-language use.**

---

### 3. Account and Customer Number Classification in Financial and ERP Systems

**Result: SAP and Oracle use numeric account number ranges as the primary classification mechanism. Alphanumeric account codes exist but the range comparison is internal to configuration tooling, not exposed as a user-authored business rule condition.**

#### SAP GL account groups and number ranges

SAP FICO uses **account groups** with assigned number ranges to classify GL accounts. Example from SAP documentation:
> "Share Capital GL accounts range from 100000 to 100999, Reserves and Surpluses from 101000 to 101999."

Each account group has a from/to number range defined in Customizing. When a GL account is created, the system validates its number falls within the assigned group's range. SAP GL accounts are **numeric by default**; alphanumeric is supported but practitioners are advised not to mix.

The SAP community question "GL Account Number Range in Alphabetic" exists, confirming that practitioners who want alphabetical account codes have to explicitly configure alphanumeric support — and the system's default tooling treats ranges as numeric intervals, not string comparison expressions.
- Sources: https://community.sap.com/t5/enterprise-resource-planning-q-a/gl-account-group-number-ranges/qaq-p/12306728, https://www.sastrageek.com/post/mastering-sap-fico-the-importance-and-creation-of-account-groups

#### Oracle Financials account classification rules

Oracle Financials provides account classification rules with structured interval specification. The documentation explicitly mentions a `Between` operator for specifying account ranges and `HighValueChar` for the upper bound when using Between. Account segment values are classified by ranges: "1000 to 1999 might be the range of segment values for assets."
- Source: https://docs.oracle.com/en/cloud/saas/sales/20c/facmi/credit-rollup-classification-and-assignment-rules.html

#### Character of the use case

In both SAP and Oracle, account ranges are system-configuration data: they define how accounts are organized, not business rules evaluated against runtime data. The domain expert who sets up a chart of accounts specifies number ranges in a configuration table. This is categorically different from a business rule like `if account.code < "G" then apply rate tier A` — the ERP system maps account-to-group implicitly at account creation time.

**Assessment:** The concept of account number ranges as classification boundaries is real and pervasive in ERP systems. But the interaction model is always: (1) configure ranges in a data table, (2) system assigns accounts to groups automatically. Domain experts do not write `<`/`>` predicates on account code strings in business rule expressions. The range is stored as structured from/to data, and the system does the range-membership check internally.

**Evidence strength: Strong confirmation that account number ranges matter as a business concept; weak evidence that domain experts write string ordering predicates in rule languages.**

---

### 4. Geographic Zoning by Postal Code

**Result: Postal code ranges are a genuine and widely-used feature in e-commerce shipping and insurance territory rating, but the dominant implementation is range specification (from/to or wildcard), not rule-expression string ordering.**

#### E-commerce shipping (WooCommerce, Shopify, Intuitive Shipping)

**WooCommerce** supports postal code ranges using ellipsis notation: `10001…10010` matches all ZIP codes from 10001 to 10010, including non-existent ones. Wildcards (`902*`) are also supported. This is a **range input field** in the shipping zone configuration UI, not a filter condition expression. The underlying comparison is string-based (lexicographic for alphanumeric postal codes like UK postcodes).
- Sources: https://woocommerce.com/document/setting-up-shipping-zones/, https://octolize.com/blog/woocommerce-shipping-based-on-zip-code-postcode-postal-code/

**Intuitive Shipping** supports `10110:10115` range notation and wildcard matching for postal code sub-zones. UK postal code ranges use `AB10AA:AB99ZZ` format (full postcode without spaces).
- Source: https://www.help.intuitiveshipping.com/article/create-shipping-subzones-based-on-postal-zip-code/

**ShipperHQ** allows postal code conditions with operators including `CONTAINS`, `IS ONE OF`, `STARTS/ENDS WITH` in shipping zone configuration.
- Source: https://docs.shipperhq.com/zone-configuration

#### Insurance underwriting territory rating

Insurance carriers use ZIP codes extensively for territorial rating of auto and property insurance. All states permit territorial rating; some states restrict it (California, Michigan prohibit ZIP-based auto rating). The industry practice is:
1. File a territory table mapping ZIP codes to territories with the state insurance department
2. Rate lookup queries territory from ZIP
3. Territory determines rate factor

This is a lookup-table model. The "rule" is: find the territory for this ZIP code, apply that territory's rate. The implementation is a ZIP→territory mapping table, not a rule expression `zipCode >= "10000" and zipCode < "11000"`.
- Sources: https://www1.wsrb.com/blog/zip-code-insurance-rating-part-2, https://www.cga.ct.gov/2006/rpt/2006-R-0542.htm

#### USPS / UPS / FedEx zone calculation

Carrier zone calculation uses ZIP code prefix lookups (first 3 digits) to determine shipping zone from origin to destination. UPS and FedEx publish downloadable zone charts by origin ZIP code prefix. This is a lookup process, not a range expression. The "calculation" is: strip the first 3 digits of the destination ZIP, look up the zone from the carrier's zone chart.
- Source: https://www.shipbob.com/ecommerce-shipping/shipping-zones/

**Assessment:** Postal code ranges are a real, active, and well-supported business feature. However, the implementation is consistently either:
(a) A range input field (from/to) in a UI — e.g., WooCommerce's `10001…10010`
(b) A wildcard match — e.g., `902*` for all ZIPs starting with 902
(c) A lookup table indexed by ZIP or ZIP prefix

No evidence of insurance underwriters or e-commerce operators authoring business rules using `zipCode >= "10000"` as a predicate expression. When a rule-language expression is needed (e.g., in a more advanced shipping rules plugin), the operator surfaces as `STARTS WITH` or `IS ONE OF`, not `<`/`>`.

**Evidence strength: Strong confirmation that postal code range logic is a genuine business need; no evidence that domain experts express it as string ordering predicates.**

---

### 5. Document and Case Number Classification

**Result: Structured case numbering schemes exist in government and legal contexts, but classification by number range uses lookup tables and routing rules, not string ordering predicates.**

#### Court case management systems

US federal district courts use structured case number formats: court identifier + year + case type code + sequential number (e.g., `2:24-cv-01234`). The format includes the year explicitly, so "cases from 2020" is identified by the year component, not by string ordering on the full case number. Classification is by the type code (CV, CR), not by range ordering.

State court systems (Indiana, California) have similar structured formats with year and type codes embedded in the number. No evidence of business rules like "case numbers 2020-001 through 2020-500 use old procedure" as a system-evaluated predicate.
- Sources: https://rules.incourts.gov/Content/admin/rule8/current.htm, https://www.scd.uscourts.gov/Filing/casenum.asp

#### Government permit systems

Permit numbering follows jurisdiction-specific patterns. Sonoma County's permit numbers are structured with a 3-character type code, year, and sequential number. A rule like "permits filed before July 1999 use old procedure" is implemented by splitting on the date component of the number or by a historical date field, not by string ordering on the full permit number.
- Source: https://permitsonoma.org/permitservices/permitsonline/permitnumberformatinfo

**Assessment:** The concept of number ranges for procedural classification exists, but the implementation always parses the year or type code component from a structured identifier rather than applying `<`/`>` to the full string. String ordering on a composite identifier like `2024-cv-01234` would not produce correct results because the year digit changes the ordering unexpectedly.

**Evidence strength: Weak. The concept exists but the implementation pattern avoids string ordering in favor of structured field extraction.**

---

### 6. CRM and Marketing Segmentation

**Result: No evidence of alphabetical-range segmentation in any major marketing automation platform. Name-range segmentation is not a documented use case.**

#### Mailchimp segmentation

Mailchimp's segment conditions for contact fields include `is`, `is not`, `contains`, `does not contain`, `starts with`, `ends with`, `is blank`, `is not blank`. No `less than` or `greater than` for text fields. No evidence of Mailchimp users requesting alphabetical segmentation.
- Source: https://mailchimp.com/help/all-the-segmenting-options/

#### Marketo segmentation

Marketo supports up to 5 layers of segmentation with text and activity conditions. No evidence of alphabetical range conditions in Marketo's documented filter types.

#### Salesforce Marketing Cloud

Not directly investigated in this search, but given the pattern across all other CRM platforms, the expectation is: text field conditions support equality, contains, and starts-with — not ordering.

**Assessment:** Marketing segmentation by alphabetical name range does not appear in any platform documentation or community discussion. The use case itself is weak: segmenting "customers whose last name comes before M" has no marketing rationale — there is no business reason to think people named A-M are different from people named N-Z as a marketing segment.

**Evidence strength: No demand signal found.**

---

### 7. No-Code / Workflow Automation Platforms

**Result: Zapier's `(Text)` filters do not include `less than` / `greater than`. Power Automate's `greater` function works on strings but is documented as confusing and discouraged. Make/Integromat does not surface string ordering as a filter option. None of the major no-code platforms treats string `<`/`>` as a first-class user-facing condition.**

#### Zapier

Zapier's filter conditions are typed by data type. The `(Text)` rule type supports: `Contains`, `Does Not Contain`, `Exactly Matches`, `Does Not Exactly Match`, `Is In`, `Is Not In`, `Starts With`, `Does Not Start With`, `Ends With`, `Does Not End With`, `Exists`, `Does Not Exist`. The `(Number)` rule type supports `Greater Than`, `Less Than`, `Greater Than or Equal To`, `Less Than or Equal To`. There is a strict data-type separation: `Greater Than` is a **number** operator, not a text operator.
- Sources: https://help.zapier.com/hc/en-us/articles/8496180919949-Filter-and-path-rules-in-Zaps, https://zapier.com/blog/filter-by-zapier-guide/

A Zapier community post asking about "greater than" on search results confirms the limitation: the platform restricts ordering operators to numeric fields.

#### Power Automate

Power Automate's expression language has `greater()` and `less()` functions that technically accept strings. However, official Microsoft documentation and the community consistently warns that string comparisons using `greater()`/`less()` are "hard to understand" and produce results that are "pretty hard to understand the outcome, so there's no real reason to compare strings this way." A community forum thread titled "Flow is Not Handling Greater Than/Greater Than or Equal" (2024) shows a user experiencing unexpected behavior with string ordering — confirming the feature is technically present but practically problematic.

The GUI condition builder in Power Automate does not expose `<`/`>` for text fields — it only exposes them through the expression editor. This means the feature is developer-accessible but not domain-expert-accessible.
- Sources: https://powerusers.microsoft.com/t5/General-Power-Automate/strings-comparison-using-condition-action-in-Power-Automate/td-p/2091158, https://debajmecrm.com/string-comparison-not-working-in-power-automate-know-when-it-can-fail/

#### Make (Integromat)

Make's filter conditions support numeric comparison operators but the available evidence shows text/string comparisons use `text:contains`, `text:startswith`, and similar substring operators — not ordering. No documented `less than` / `greater than` for text fields in Make's filter UI.
- Source: https://www.integromat.com/en/help/filtering

#### Airtable

Airtable's formula language technically supports `<`, `>`, `<=`, `>=` on text values (codepoint-based comparison). However, the official documentation and community discussion focuses on numeric uses. No community thread shows users using Airtable string ordering for a business condition. Airtable's filter UI does not expose `<`/`>` for text fields.
- Source: https://support.airtable.com/docs/formula-field-reference

#### Monday.com, Notion, AppSheet

AppSheet explicitly says `5 > "Hello"` is not valid — the platform does not allow comparison operators between different types. Text field filtering uses `CONTAINS()` and exact match.

Notion formulas do support comparison operators on text, but through normalization functions (`toLowerCase(a) < toLowerCase(b)`) — not a built-in CI comparison operator.

**Overall no-code assessment:** Across the major no-code/workflow platforms:
- Zapier: text `<`/`>` not available as a condition type
- Power Automate: technically available via expressions but documented as problematic and not surfaced in the GUI condition builder
- Make: not surfaced as a text condition
- Airtable: formula language supports it but not surfaced in filter UI; no community demand examples
- AppSheet: explicitly not supported across types
- Notion: manual normalization required

**Evidence strength: No meaningful demand signal. The absence of string ordering in the GUI of every major no-code platform is strong negative evidence that domain experts are not requesting this feature.**

---

### 8. Business Rules Engines (General)

**Result: Drools explicitly supports string `<`/`>` by documentation, but no non-healthcare real-world examples of this being used for business routing or classification were found.**

#### Drools DRL

Drools documentation explicitly states: "For String fields, the `<` operator means alphabetically before." The example given is `persons[ firstName < $otherFirstName ]` — comparing person names. The feature is intentional and documented.

However, the prior research file (`string-ordering-external-survey.md`) found no real-world healthcare examples, and this broader survey found no real-world manufacturing, insurance, financial, or CRM examples using Drools string ordering either. The capability exists in the engine but no public examples of domain experts using it for business routing or classification were found.
- Source: https://docs.drools.org/8.39.0.Final/drools-docs/docs-website/drools/language-reference/index.html

#### FEEL / DMN (Camunda)

FEEL theoretically supports string range comparison by the OMG spec, but Camunda's JavaScript FEEL engine (`feelin`) has a known, open bug where multi-character string ranges fail (GitHub issue #73). Single-character ranges work; multi-character ranges do not. No evidence of general-business practitioners using FEEL string ranges outside healthcare.

#### Pega

Pega rules use a combination of Java expressions and Pega's own decision table / condition builder. No evidence of string ordering conditions in publicly-documented Pega business rules.

**Evidence strength: String ordering exists in developer-facing rules engines (Drools, FEEL spec) but no domain-expert usage evidence across any business domain was found.**

---

### 9. Financial Security Identifiers (CUSIP, ISIN, Ticker Ranges)

**Result: No evidence of range-based business rules on security identifiers. These are point-lookup identifiers, not ordered classifications.**

CUSIP (9 characters), ISIN (12 characters), and ticker symbols are identifier systems designed for point lookup (find security X), not for range classification. CUSIP's first 6 characters identify the issuer; the 7th-8th identify security type; the 9th is a check digit. There is no business meaning to "CUSIPs less than X50000" — the character ordering has no domain semantics.

No evidence found of investment rules engines, risk systems, or compliance systems that classify securities using string range predicates on CUSIP/ISIN identifiers.

**Evidence strength: No demand signal. The domain semantics do not support string range ordering on these identifiers.**

---

### 10. Logistics and Freight Classification

**Result: Freight classification uses numeric class codes (Class 50 through Class 500) and NMFC numeric codes — not string ordering.**

Freight classification (LTL) uses the NMFC (National Motor Freight Classification) system. Items are assigned to freight classes (50, 55, 60, 65, 70, 77.5, 85, 92.5, 100, 110, 125, 150, 175, 200, 250, 300, 400, 500). Classification is based on density, handling, stowability, and liability — not on alphanumeric identifiers. The class codes are numeric.

Container numbers (4 letters + 7 digits format) are identifiers for individual containers, not classification codes. No evidence of range-based business rules on container numbers.

Bill of lading classification relies on NMFC numeric codes. No string ordering use cases found.

**Evidence strength: No demand signal. The domain uses numeric classification, not string ordering.**

---

### 11. Education (Course Numbers, Student IDs)

**Result: Course number ranges as classification categories are a real convention, implemented as numeric ranges with documented standard semantics.**

US universities use course number ranges to classify courses by level:
- 1000–1999: introductory undergraduate
- 2000–2999: lower-level undergraduate
- 3000–3999: upper-level undergraduate
- 4000–4999: advanced undergraduate
- 5000–5999: graduate level

These ranges are real classification categories. However, the course numbers are **numeric** (despite being called "course numbers" — they are numeric values). The comparison is numeric, not string. No evidence of educational systems writing string predicates like `courseNumber < "3000"`.

**Evidence strength: The classification concept is real but numeric; no string ordering use case.**

---

### 12. Real Estate (Assessor Parcel Numbers)

**Result: Assessor parcel numbers (APNs) are jurisdiction-specific identifiers combining book/page/parcel or section/block/lot. Range-based classification is not documented as a rule-expression pattern.**

APNs are structured composite identifiers varying by jurisdiction. New York uses SBL (section/block/lot) numbers. Los Angeles uses book-page-parcel. Range classification would require parsing the composite structure, not applying a raw string comparison. No evidence of rule expressions using APN range predicates.

**Evidence strength: No demand signal.**

---

## Cross-Cutting Synthesis

### Pattern 1: The "from/to range input" vs. "rule predicate" distinction

The most important finding across all categories is a consistent architectural distinction: **range specification as structured data input** versus **range comparison as a rule predicate**.

| Interaction model | Example | String `<`/`>` as operator? |
|---|---|---|
| Range input field (from/to) | SAP account group number range; Oracle recall notice serial range; WooCommerce postal code range | No — range stored as data, system evaluates internally |
| Lookup table | Insurance territory-by-ZIP table; carrier zone chart; ERP account-to-group mapping | No — membership lookup, not comparison operator |
| Named rule condition with starts-with / wildcard | ShipperHQ `STARTS WITH`; WooCommerce `902*` wildcard | Partial — operator is not `<`/`>` but tests a prefix boundary |
| Rule predicate expression | Drools DRL; Precept | Yes — but no domain-expert examples found in practice |

The gap is significant: domain experts consistently interact with range logic through specialized UIs that expose from/to input fields, not through general-purpose `<`/`>` operators in rule expressions.

### Pattern 2: The substitute operators

When domain experts do express range-like conditions in workflow and rule tools, the operators they reach for are:
1. `starts with` / `begins with` — for prefix-based ranges (postal code prefix, code chapter prefix)
2. `is in` / `is one of` — for explicit enumeration
3. Numeric `<`/`>` — when the "string" is really numeric (account numbers, course numbers)
4. `contains` / `matches` — for pattern matching on structured codes

**No evidence was found of domain experts reaching for `<`/`>` on text fields when available, or requesting it when unavailable, in any of the twelve categories surveyed.**

### Pattern 3: Developer vs. domain-expert tooling

Where string `<`/`>` on text exists, it is in developer-facing tools:
- Drools DRL (Java-based engine, developer-authored rules)
- Power Automate expression language (requires opening the expression editor, bypassing the GUI)
- SQL (developer/analyst-facing query language)
- Excel formulas (power user feature, codepoint-based, not alphabetically intuitive)

The GUI-level, domain-expert-facing surfaces — Zendesk triggers, Salesforce workflow rules, HubSpot conditions, Zapier filters, Make filters, AppSheet — uniformly omit string `<`/`>` as a condition type.

This is a strong negative signal: the platforms whose design principle is "domain expert authors the rule" have collectively concluded that string `<`/`>` is not a construct domain experts need.

### Pattern 4: Where string ranges are genuine but handled differently

Two categories emerged with genuine, well-documented real-world need for range logic on string-typed identifiers:

**Serial/lot number recall ranges (manufacturing/pharma):** Oracle and SAP provide structured range specification in recall notices because this is a real business operation. The implementation is always a dedicated recall notice data model with from/to serial number fields, not a general-purpose rule predicate. Domain experts fill in a form; the system does the range query.

**Postal code ranges for shipping zones:** WooCommerce, Intuitive Shipping, and other e-commerce platforms support postal code ranges explicitly. The input syntax (`10001…10010`, `10110:10115`, `902*`) is domain-specific, not a general string `<`/`>` operator.

**The implication for Precept:** If a Precept author needs to express "recall covers units with serial numbers in range X to Y" or "shipping rate applies to postal codes in range A to B," the idiomatic Precept approach would be a dedicated constraint type or a `between(field, low, high)` function — not general-purpose `<`/`>` operators. The domain experts in these use cases interact through range fields, not ordering predicates.

---

## Implications for Precept

### Precept is correctly positioned

Precept's primary author is a domain expert. The evidence from this survey is that domain experts, across all business domains surveyed, do not write `<`/`>` on text fields as business rules. The platforms designed for domain-expert rule authoring have collectively omitted this feature. This is convergent evidence that the Precept language specification is correct to not surface string `<`/`>` as a first-class feature.

### The substitutes Precept already has

For the cases where range logic on string-typed data is a genuine need:

| Use case | Precept's available mechanism |
|---|---|
| Postal code prefix ranges | `startsWith(zipCode, "902")` |
| Serial number range (recall) | `between(serialNumber, "SN-10000", "SN-19999")` — if added as a function; or numeric conversion if serial numbers are digits |
| Account number classification | Numeric `<`/`>` (if account codes are numeric); or `is in [...]` for explicit sets |
| Alphabetical routing | Explicit set membership `lastName starts with ["A","B","C"...]`; or ordered-choice field type |

### The `between` function case

The serial number recall use case is arguably the strongest real-world signal found in this survey. Oracle and SAP explicitly model serial number ranges. If a Precept precept governs a serialized product entity, a rule like `serialNumber between "SN-10000" and "SN-19999"` could be a legitimate authoring need.

However, this is a `between(a, b, c)` function argument, not evidence for free-standing `<`/`>` operators. The function form is safer because it explicitly names the range endpoints and avoids the cascading-operator problem. It is also how Oracle and SAP model it at the data level (from/to fields).

### The `starts with` adequacy question

For postal codes and serial number prefix-based ranges, `startsWith()` covers a large fraction of cases. The cases not covered are sub-prefix ranges (e.g., ZIP codes 90210–90250, where `startsWith("902")` overcaptures). For these, the domain expert's current alternative is an explicit list — which is how most real-world systems handle it (payer policy code lists, WooCommerce explicit ZIP lists).

---

## Conclusions

### Conclusion 1: No business domain provides strong evidence for string `<`/`>` as a domain-expert-facing rule operator

**Rationale:** Twelve business domains were surveyed. In every domain where range logic on string-typed data exists (serial numbers, postal codes, account codes), the dominant implementation is structured range data (from/to fields), lookup tables, or prefix matching — not `<`/`>` operators in a rule expression language.

**Alternatives considered and rejected:**
- "The platforms just haven't implemented it" — rejected because several platforms (Drools, Power Automate) do support it but no domain-expert examples were found using it.
- "Practitioners are using it but not posting about it" — rejected because targeted searches of community forums and practitioner resources found zero examples across any domain.
- "The lack of platform support suppresses demand" — partially valid, but the substitute patterns (starts-with, explicit lists, from/to fields) are consistently sufficient and preferred.

**Precedent:** Every major domain-expert-facing platform surveyed (Zendesk, Salesforce, HubSpot, Zapier, Make, D365, ServiceNow) omits string `<`/`>` as a condition type. Developer-facing platforms include it but document its behavior as confusing or non-obvious.

**Tradeoff accepted:** If string `<`/`>` is not added to Precept, authors who need a serial number range recall rule must use `between()` or `startsWith()`. These are more descriptive and less error-prone than raw string ordering.

---

### Conclusion 2: The serial number recall / postal code range use cases are real, but best served by `between()` or domain-specific constructs, not general string ordering

**Rationale:** Oracle and SAP explicitly document serial number ranges as a recall management concept. WooCommerce and Intuitive Shipping explicitly support postal code ranges. These are real operational needs. But in both cases, the system provides a dedicated interaction model (from/to fields, range syntax), not a general `<`/`>` predicate.

**Alternatives considered and rejected:**
- General `<`/`>` operators — rejected because they don't match how practitioners think about range specification (they think "from SN-10000 to SN-19999," not "serial number >= 'SN-10000' AND serial number <= 'SN-19999'").
- No support at all — rejected as insufficient for practitioners who need to express recall coverage in a Precept precept.

**Precedent:** Oracle's recall management API, SAP's serial number range input, WooCommerce's range notation all model ranges as structured from/to data, not ordering predicates.

**Tradeoff accepted:** A `between()` function is slightly less composable than raw `<`/`>` operators (it does not allow `>= x` without an upper bound). If future evidence suggests unbounded range predicates are needed, an operator surface can be considered at that point.

---

### Conclusion 3: The alphabetical routing use case is folklore, not a documented platform feature or practitioner demand signal

**Rationale:** Alphabetical name partitioning (A-M / N-Z routing) is conceptually simple and is mentioned in lead-routing tutorials as one possible approach. But no CRM, helpdesk, or workforce management platform has built it as a native condition, and no community forum posts show practitioners requesting or workarounding the absence of string `<`/`>` for this purpose.

**Precedent:** Lead routing systems use round-robin, capacity-based, and skills-based routing as the standard approaches. Name-range partitioning is a legacy practice from manual sorting workflows (e.g., alphabetical filing cabinets) that has not translated into automated routing rule demands.

**Tradeoff accepted:** If a Precept author wants to partition customers alphabetically, they can use an explicit `is in [...]` condition or a computed bucket field. These are more expressive and less fragile than raw alphabetical comparison.

---

### Conclusion 4: The no-code/workflow platform evidence is strongly negative

The collective design decision of Zapier, Make, HubSpot, ServiceNow, Zendesk, Salesforce, D365, and AppSheet — all of which have designed for domain-expert rule authoring — to omit string `<`/`>` as a condition operator is significant convergent evidence. These are not platforms that lack the technical capability (all run on infrastructure that supports string comparison); they have made a deliberate product decision to present string ordering as not meaningful in a business rule context.

This is consistent with Power Automate's documentation, which technically supports string `greater()`/`less()` but explicitly discourages using it on strings, calling the results "hard to understand."

---

## Open Questions

1. **SAP alphanumeric account code range configuration:** SAP supports alphanumeric GL account numbers, and the community question about alphabetic ranges indicates practitioners do encounter this. Does SAP's account group range configuration handle alphanumeric ranges correctly (using string comparison), and do practitioners write rules against alphanumeric account codes in SAP workflow / business rule tools?

2. **Oracle between operator for string account segments:** Oracle's account classification rule documentation mentions a `Between` operator with `HighValueChar`. Does this operator surface in a user-facing rule authoring tool, or only in configuration tables? What is the authoring experience?

3. **The `between()` function in Precept:** If the serial number recall use case becomes concrete (a Precept author governs a serialized product entity and needs to express recall coverage), is `between(serialNumber, "SN-10000", "SN-19999")` the right surface? This requires further design consideration — `between` on strings has the same ordinal-correctness dependency as `<`/`>`.

4. **WooCommerce postal code range semantics:** WooCommerce's ellipsis range `10001…10010` performs string comparison, not numeric comparison. This means `10009 < 10010` correctly, but `10100 < 10010` is false because "101" > "100" lexicographically. Do WooCommerce users encounter range errors from this? A community investigation of WooCommerce postal code range bugs might reveal whether practitioners understand they're getting string comparison behavior.

5. **Power Automate community demand:** The community forum thread "Flow is Not Handling Greater Than/Greater Than or Equal" (2024) indicates unexpected behavior with string comparison. What was the user's actual use case? If the underlying need was numeric comparison on a text field (a common workaround pattern), this is different from a genuine string ordering use case.

---

## Sources

- Zendesk ticket trigger conditions reference — https://support.zendesk.com/hc/en-us/articles/4408893545882-Ticket-trigger-conditions-and-actions-reference
- Salesforce workflow rule criteria definition — https://help.salesforce.com/s/articleView?id=sf.workflow_rules_define.htm
- Microsoft D365 Customer Service assignment methods — https://learn.microsoft.com/en-us/dynamics365/customer-service/administer/assignment-methods
- HubSpot community: property comparison in workflows — https://community.hubspot.com/t5/CRM/Is-there-a-method-to-Compare-user-properties-using-workflows-and/m-p/397158
- Oracle Product Recall Management (serial number ranges) — https://docs.oracle.com/en/cloud/saas/supply-chain-and-manufacturing/25b/fampr/locate-recalled-parts.html
- SAP serial number traceability community — https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-members/serial-number-traceabilty/ba-p/12953411
- FDA DSCSA product tracing requirements — https://www.fda.gov/drugs/drug-supply-chain-security-act-dscsa/drug-supply-chain-security-act-product-tracing-requirements-frequently-asked-questions
- FDA FSMA food traceability rule — https://www.fda.gov/food/food-safety-modernization-act-fsma/fsma-final-rule-requirements-additional-traceability-records-certain-foods
- SAP GL account group number ranges — https://community.sap.com/t5/enterprise-resource-planning-q-a/gl-account-group-number-ranges/qaq-p/12306728
- SAP account group mastering — https://www.sastrageek.com/post/mastering-sap-fico-the-importance-and-creation-of-account-groups
- Oracle Financials account classification rules — https://docs.oracle.com/en/cloud/saas/sales/20c/facmi/credit-rollup-classification-and-assignment-rules.html
- WooCommerce shipping zones (postal code range syntax) — https://woocommerce.com/document/setting-up-shipping-zones/
- WooCommerce postal code range (Octolize guide) — https://octolize.com/blog/woocommerce-shipping-based-on-zip-code-postcode-postal-code/
- Intuitive Shipping postal code sub-zones — https://www.help.intuitiveshipping.com/article/create-shipping-subzones-based-on-postal-zip-code/
- ShipperHQ zone configuration operators — https://docs.shipperhq.com/zone-configuration
- WSRB ZIP code insurance rating — https://www1.wsrb.com/blog/zip-code-insurance-rating-part-2
- Connecticut territorial auto insurance report — https://www.cga.ct.gov/2006/rpt/2006-R-0542.htm
- ShipBob shipping zones guide — https://www.shipbob.com/ecommerce-shipping/shipping-zones/
- Indiana court rule 8 uniform case numbering — https://rules.incourts.gov/Content/admin/rule8/current.htm
- Sonoma County permit number format — https://permitsonoma.org/permitservices/permitsonline/permitnumberformatinfo
- Mailchimp segmentation options — https://mailchimp.com/help/all-the-segmenting-options/
- Zapier filter conditions reference — https://help.zapier.com/hc/en-us/articles/8496180919949-Filter-and-path-rules-in-Zaps
- Zapier filter guide — https://zapier.com/blog/filter-by-zapier-guide/
- Power Automate string comparison community — https://powerusers.microsoft.com/t5/General-Power-Automate/strings-comparison-using-condition-action-in-Power-Automate/td-p/2091158
- Power Automate string comparison failures — https://debajmecrm.com/string-comparison-not-working-in-power-automate-know-when-it-can-fail/
- Power Automate community: greater than / string — https://community.powerplatform.com/forums/thread/details/?threadid=f636a245-fae7-42db-8099-b9f17339ed46
- Encodian compare text in Power Automate — https://www.encodian.com/blog/compare-text-and-strings-in-power-automate/
- Make (Integromat) filtering — https://www.integromat.com/en/help/filtering
- Airtable formula field reference — https://support.airtable.com/docs/formula-field-reference
- AppSheet Yes/No expressions — https://support.google.com/appsheet/answer/10107946
- Drools Rule Language Reference (string `<` documentation) — https://docs.drools.org/8.39.0.Final/drools-docs/docs-website/drools/language-reference/index.html
- Manufacturing batch number and recall — https://www.symestic.com/en-us/what-is/batch-number
- NMFC freight classification guide — https://arcb.com/blog/freight-class-and-nmfc-code-basics
- CUSIP identifier structure — https://www.cusip.com/identifiers.html
- University of Virginia course numbering scheme — https://registrar.virginia.edu/faculty-staff/course-numbering-scheme
- Assessor's parcel number (Wikipedia) — https://en.wikipedia.org/wiki/Assessor's_parcel_number
