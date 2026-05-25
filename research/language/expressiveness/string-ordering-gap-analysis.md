# String Ordering Gap Analysis: Cases Not Covered by Ordered Choice

> **Research question:** Are there valid, non-CI business use cases for `string < string` (`<`, `>`, `<=`, `>=`) on open-ended string fields that are not already covered by `choice of string(...) ordered`, `startsWith`, or existing numeric/temporal types?
>
> **Context:** Three prior research files analyze string ordering ([`business-string-ordering-use-cases.md`](./business-string-ordering-use-cases.md), [`string-ordering-vs-ordered-choice.md`](./string-ordering-vs-ordered-choice.md), [`string-ordering-architectural-analysis.md`](./string-ordering-architectural-analysis.md)). Those files conclude against implementation, but the CI use-case survey is the primary evidence base. This file specifically targets the non-CI, ordinal case to check for gaps the prior research may have missed.
>
> **Date:** 2026-05-24

---

## Background

The prior research argues: ordered choice covers discrete ranks; dedicated types cover structured data; display sort is a UI concern. That framing handles priority/severity/status fields well but does not engage deeply with a distinct category: **standardized code systems** where the value space is too large for ordered choice but the format guarantees ordinal == domain ordering.

The corpus already contains these fields:

| Field | Sample | Standard | Format |
|-------|--------|----------|--------|
| `PrimaryDiagnosisCode as string maxlength 10` | `utilization-review-case`, `patient-referral-management`, `medical-prior-auth` | ICD-10-CM | Letter + 2 digits + optional `.` + digits |
| `PostalCode as string maxlength 10` | `patient-enrollment` | US ZIP | 5 digits (zero-padded) or ZIP+4 |
| `ServicePostalCode as string maxlength 20` | `utility-service-connection` | Mixed | Country-dependent |
| `ProcedureCode as string maxlength 20` | `medical-prior-auth`, `prior-auth-appeal` | CPT-4 | 5 digits |
| `ICD10Codes as set of string` | `prior-auth-appeal` | ICD-10-CM | Same as above |

None of the corpus rules currently use range comparisons on these fields. That could mean (a) range rules aren't needed, or (b) authors skip them because the feature doesn't exist.

---

## The Genuine Gap: Fixed-Format Standardized Codes

### What ordered choice cannot model

Ordered choice requires the author to enumerate every member at declaration time. These systems are too large:

| System | Size | Example | Ordered choice viable? |
|--------|------|---------|----------------------|
| ICD-10-CM | ~72,000 codes | C50.111 (right upper-outer breast, female) | **No** — enumeration is impossible |
| CPT-4 | ~10,000 codes | 99213 (office visit, established patient) | **No** |
| NAICS 2022 | ~1,100 codes | 522110 (Commercial Banking) | **Borderline** — could enumerate but fragile to updates |
| US ZIP codes | ~42,000 | 10001 (Manhattan midtown) | **No** |
| HCPCS Level II | ~7,000+ | J0171 (Adrenalin injection) | **No** |

### Where ordinal comparison gives correct results

These systems share a structural property: **fixed-width, zero-padded, prefix-organized**. This means ordinal comparison equals domain ordering.

**ICD-10-CM chapter boundaries** (letter prefix encodes chapter):

| Range | Chapter | Ordinal correct? |
|-------|---------|-----------------|
| A00–B99 | Infectious and parasitic diseases | ✅ `Code >= "A" and Code < "C"` |
| C00–D49 | Neoplasms | ✅ `Code >= "C" and Code < "D5"` |
| D50–D89 | Blood diseases | ✅ `Code >= "D5" and Code < "E"` |
| E00–E89 | Endocrine/metabolic | ✅ `Code >= "E" and Code < "F"` |

**Real payer rule example:** "Prior authorization required for all neoplasm diagnoses in chapters C00–D49." The business rule is a range over a standardized code system. The domain expert knows the chapter boundary. Ordinal comparison gives the correct result because ICD-10 prefixes are alphabetical and the numeric portion is zero-padded.

**Where `startsWith` covers it:** Chapter-level rules (`startsWith("C") or startsWith("D")`) work but require listing individual prefixes. A contiguous range across chapter boundaries — "C00 through D49" — requires either: (a) enumerating both prefixes, or (b) a range expression. Both are correct but the range expression is more readable and less error-prone.

**Where `startsWith` fails:**

Sub-chapter ranges that don't align with a single prefix:

- "C50–C58 (breast malignancies)" → `startsWith("C5")` catches C50-C59, not C50-C58. An author writing this as `startsWith` makes a subtle error.
- "D00–D09 (in situ neoplasms)" → `startsWith("D0")` catches D00-D09. ✅ Works here.
- "D37–D48 (uncertain behavior)" → `startsWith("D3") or startsWith("D4")` — messy and still wrong (D30-D36 are benign, D49 is unspecified). Range is cleaner: `Code >= "D37" and Code <= "D48"`.

**US ZIP codes:**

| Use case | Range expression | `startsWith` equivalent | Equivalent? |
|----------|-----------------|------------------------|-------------|
| New York metro area | `ZipCode >= "10000" and ZipCode <= "10999"` | `startsWith("1")` | ❌ `startsWith("1")` also matches 10000-19999 |
| New England | `ZipCode >= "01000" and ZipCode <= "02999"` | `startsWith("0")` | ❌ `startsWith("0")` also matches 03000-09999 |
| Specific borough | `ZipCode >= "10001" and ZipCode <= "10199"` | No clean equivalent | ❌ needs ordering |

ZIP+4 (`NNNNN-NNNN` format with hyphen) complicates ordinal comparison slightly, but the 5-digit form is standardized, uniformly zero-padded, and ordinal == geographic ordering within postal districts.

---

## What the Prior Research Got Right (and Where It Applies)

The prior research's "use ordered choice or dedicated type" verdict IS correct for:

| Case | Why prior research is correct |
|------|------------------------------|
| Priority: Low/Medium/High | Finite, known — ordered choice is strictly better |
| Severity: P1/P2/P3 | Same |
| Version strings "1.10" < "1.9" | Ordinal gives wrong result — dedicated type needed |
| Room numbers "2" < "10" | Ordinal gives wrong result — numeric type or zero-padding required |
| Arbitrary names "apple" < "Banana" | Encoding artifact, not business meaning — UI concern |
| Date strings (if using date type) | Precept has `date`, `time`, `instant` types |
| Number-as-string "100" < "9" | Wrong algorithm — use integer |

None of these cases have the fixed-format, zero-padded, standards-body-defined property.

---

## Assessment of the Gap

### Is it real?

**Yes.** The corpus contains ICD-10 and postal code fields. Real payer and insurance systems write rules against code ranges. The rules are not present in the current corpus because the feature doesn't exist — not because the need doesn't exist.

Three confirmed cases:
1. **ICD-10 range rules in payer systems** — "prior auth required for C00-D49"; "exclude D37-D48 (uncertain behavior codes) from auto-approval"
2. **ZIP code service area rules** — "accept enrollment only in ZIP codes 10001-10199"; "rate tier applies to ZIPs 90000-90299"
3. **CPT code range rules** — "surgical codes 10004-69990"; "E/M codes 99202-99215 require level documentation"

### Is ordered choice ever sufficient?

Only when the domain-specific subset is finite AND stable. For regulatory code systems, the answer is almost always no — the code tables are updated annually by standards bodies.

### Does `startsWith` cover most real cases?

**Partial coverage.** Chapter-level rules (coarsest granularity) work with `startsWith`. The failure cases are sub-chapter ranges that don't align with a character prefix. In practice, payer and prior-auth rules often work at sub-chapter granularity. `startsWith` is insufficient.

### What is the risk of implementing ordinal string ordering?

The risk is authors using it on non-fixed-format fields where ordinal gives wrong results:

```precept
# Author intends: "room must be in first three floors"
rule RoomNumber < "400" because "only floors 1-3 allowed"
# Actual result: "4" > "400" ordinally, so room "4" passes the rule incorrectly
```

The compiler cannot distinguish `field ZipCode as string` (fixed 5-digit) from `field RoomNumber as string` (variable format). Both would accept `< "400"` without complaint.

---

## Conclusions

### 1. There is a genuine, non-CI gap — but it is narrow and domain-specific

**Rationale:** Fixed-format standardized code systems (ICD-10, CPT, ZIP, NAICS) genuinely need range expressions, cannot be modeled as ordered choice, and have ordinal-correct ordering. The corpus already contains these fields; the business rules that go with them are simply absent because the feature doesn't exist.

**Alternatives considered and rejected:**
- `startsWith` only: covers prefix-aligned ranges but fails for sub-prefix ranges (e.g., D37-D48, ZIP 10001-10199). Authors would make subtle errors writing `startsWith` approximations.
- `toInteger()` bridge: would work for purely numeric codes (ZIP, CPT, NAICS) but ICD-10 has a letter prefix and can't be parsed to integer cleanly. Also, `toInteger()` doesn't currently exist in Precept.
- Ordered choice: infeasible for any system with thousands of codes updated annually.
- Dedicated code types (`icd10`, `zip`, `cpt`): the right long-term answer, but requires designing domain-specific types for each regulatory system. Doesn't scale to all standardized code systems.

**Precedent:** SQL systems write `WHERE diagnosis_code BETWEEN 'C00' AND 'D49'` routinely. Drools and FEEL allow `code < "D49"` on string fields. This is an accepted, real use case in healthcare and insurance rule systems.

**Tradeoff accepted:** Implementing string ordering allows misuse on non-fixed-format fields where ordinal is wrong. The compiler cannot detect this. Authors must understand that string ordering is semantically meaningful only when the field has a fixed-width, zero-padded format.

### 2. The prior research's verdict is wrong for this category, right for everything else

The recommendation to "remove string ordering, use ordered choice" is correct for:
- Discrete, author-defined ranks (priority, severity, status)
- Data that belongs in a typed field (use `date`, `integer`, `decimal`)
- Display sort (UI concern)

It is **not** correct for open-ended standardized code fields where the format guarantees ordinal correctness. Those cases exist, are in the corpus, and do not have a clean current workaround.

### 3. Recommendation — **REVISED 2026-05-24**

**Final recommendation: do not implement string ordering.**

This conclusion originally read "Do not remove string ordering from the language spec. Implement it." That recommendation was based on the theoretical gap identified in this document — fixed-format standardized code systems where ordered choice is infeasible and lexicographic equals domain ordering. The theoretical gap is real.

Two follow-on external surveys revise the recommendation:

- [`string-ordering-external-survey.md`](./string-ordering-external-survey.md) — healthcare-specific practitioner survey. Found that healthcare IT (CQL, FHIR, payer rule systems) uses **ValueSet membership**, not `<`/`>` range predicates, on standardized codes. Zero examples of domain experts writing string ordering on ICD-10 / CPT / similar codes.
- [`string-ordering-broad-use-cases.md`](./string-ordering-broad-use-cases.md) — 12-domain cross-platform survey (CRM, manufacturing, finance, logistics, e-commerce, no-code platforms, business rules engines, education, government, real-estate). Found no demand signal in any domain. Every major rule-authoring platform designed for domain experts (Zendesk, Salesforce, HubSpot, Zapier, Make, Dynamics 365, ServiceNow, AppSheet) deliberately omits string `<`/`>` from its condition surface. Where the operator exists in developer-facing engines (Drools, FEEL spec, Power Automate), the result is documented as confusing and no real-world authoring examples surface.

**Why the theoretical gap does not justify implementation:** The fixed-format code use case is real but is uniformly handled by *other* mechanisms in real systems — ValueSet membership in healthcare, from/to data fields for serial-number recalls (Oracle, SAP), prefix/wildcard matching in shipping zones (WooCommerce). Domain experts do not reach for `<`/`>` even where it is technically available. Shipping the operator in Precept would offer a feature with no observed authoring demand and would create a misuse hazard on the much-larger class of free-form text fields where lexicographic ordering produces nonsensical results (`"10" < "9"` is true).

**Implemented decision:** String `<`/`>`/`<=`/`>=` is intentionally out of scope. The per-decision rationale lives in [`docs/language/primitive-types.md` § String Ordering — Out of Scope](../../../docs/language/primitive-types.md#string-ordering--out-of-scope). The CI form (`~<`, `~>`) is also out of scope for the same reason plus the locale-ambiguity concern.

**If a concrete use case surfaces:** The right surface is either (a) a dedicated domain type (`zipcode`, `icd10`) that bakes in the format and exposes range semantics safely, or (b) a `between(field, lo, hi)` function with named endpoints. Both are cleaner than free-standing `<`/`>` on free-form `string`. Neither has demand evidence yet.

---

## Sources

- [`string-ordering-vs-ordered-choice.md`](./string-ordering-vs-ordered-choice.md) — prior research, correct for the ordered-choice case
- [`business-string-ordering-use-cases.md`](./business-string-ordering-use-cases.md) — CI ordering survey; not directly applicable to this question
- [`string-ordering-architectural-analysis.md`](./string-ordering-architectural-analysis.md) — architectural analysis; verdict applies to arbitrary strings, not fixed-format codes
- [`string-ordering-external-survey.md`](./string-ordering-external-survey.md) — healthcare-focused external survey of real-world practitioner behavior; contributes to the **revision of Conclusion 3** above.
- [`string-ordering-broad-use-cases.md`](./string-ordering-broad-use-cases.md) — broad cross-domain external survey (12 domains); contributes to the **revision of Conclusion 3** above. Together with the healthcare survey, establishes that the absence of a demand signal is universal, not healthcare-specific.
- ICD-10-CM Code Table — https://www.cms.gov/medicare/coding-billing/icd-10-codes
- NAICS Code Structure — https://www.census.gov/naics/
- USPS ZIP Code database — https://pe.usps.com/text/pub28/28apc_002.htm
- Precept corpus: `samples/utilization-review-case.precept`, `samples/medical-prior-auth.precept`, `samples/patient-enrollment.precept`
