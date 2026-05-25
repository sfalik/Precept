# String Ordering on Code Fields: External Survey

> Do business users — in real DSLs, no-code platforms, spreadsheet tools, and business rules engines — actually write ordering comparisons (`<`, `>`, `<=`, `>=`) on string fields representing standardized code systems (ICD-10, ZIP, CPT, NAICS)? This survey investigates the gap identified in [`string-ordering-gap-analysis.md`](./string-ordering-gap-analysis.md).

**Date:** 2026-05-24

---

## Background

Three prior research files concluded against string ordering for arbitrary strings ([`string-ordering-vs-ordered-choice.md`](./string-ordering-vs-ordered-choice.md), [`business-string-ordering-use-cases.md`](./business-string-ordering-use-cases.md), [`string-ordering-architectural-analysis.md`](./string-ordering-architectural-analysis.md)). Those files primarily grounded their verdicts on the case-insensitive ordering problem and on the adequacy of ordered choice for discrete ranks.

The gap analysis ([`string-ordering-gap-analysis.md`](./string-ordering-gap-analysis.md)) identified a category the prior research did not engage deeply: **fixed-format, standardized code systems** where the value space is too large to enumerate as ordered choices, but where the format is standardized and fixed-width, so ordinal comparison gives correct domain ordering. Candidate examples from the Precept corpus: ICD-10-CM diagnosis codes, US ZIP codes, CPT procedure codes, NAICS industry codes.

The gap analysis hypothesized that real payer and prior-auth systems might write rules like `diagnosisCode >= "C00" and diagnosisCode < "D50"` (ICD-10 chapter range for neoplasms) or `zipCode >= "10000" and zipCode <= "10999"` (geographic service area). This survey investigates whether that hypothesis is confirmed by external evidence.

---

## Methodology

Investigated across five platform categories, using web search and page fetches:

1. **Spreadsheet tools** (Excel, Google Sheets) — official docs, formula community threads, healthcare IT blog posts
2. **No-code platforms** (Airtable, AppSheet) — official docs, community forums
3. **Business rules engines** (Drools/DRL, FEEL/DMN, CQL/HL7) — official documentation, specification text, GitHub issues
4. **Healthcare-specific systems** (prior-auth payer policies, OpenEMR SQL, Pega Foundation for Healthcare, CMS ICD-10 guides) — payer policy documents, healthcare IT forums
5. **Power BI / DAX** — Microsoft official documentation

Searches were targeted at concrete, real-world examples: forum posts, code samples, specification text, and publicly documented payer policies. Where no concrete examples were found, the absence is noted as a finding — not assumed to mean it does not happen.

**Excluded:** CI string ordering (covered thoroughly in [`business-string-ordering-use-cases.md`](./business-string-ordering-use-cases.md)), version string anti-patterns (well-documented), date-as-string anti-patterns (well-documented), room-number anti-patterns (well-documented).

---

## Findings

### 1. Spreadsheet Tools (Excel, Google Sheets)

**Result: No evidence of string range comparison on code fields in real use.**

Excel and Google Sheets both technically support `<`, `>`, `<=`, `>=` on string values in formula expressions. The DAX documentation for Power BI explicitly lists string as a supported type for comparison operators, including examples like `[Sales Date] < "Jan 1 2009"` — but this is a date-as-string pattern, not an arbitrary code field. No forum threads, tutorials, or healthcare IT blog posts show anyone writing `=IF(A1 >= "C00", ...)` on an ICD-10 field or `=IF(B2 >= "10000", ...)` on a ZIP code field as a range check.

Community discussions about ICD-10 codes in Excel (MSDN forum thread, November 2017; Microsoft Tech Community ICD-10 thread) focus entirely on:
- Formatting codes (inserting the period separator: `LEFT(A1,3) & "." & RIGHT(A1,...)`)
- Pattern matching via `LIKE`-equivalent formulas using `FIND()` or `SEARCH()`
- Exact-match lookup via `VLOOKUP` / `XLOOKUP` against a code table

For ZIP code range questions in Airtable (community threads on ZIP code lookup and pricing by ZIP), users consistently use:
- Lookup tables with exact match
- Geographic zone partitioning (split ZIP space by leading digit into 10 tables)
- Linked record fields

The BMC Medical Research Methodology paper on Excel-based ICD-10 comorbidity calculators (Quan/Charlson indices) uses `LEFT()` and `MID()` string functions to extract code prefixes and match them against lookup arrays — not `<` or `>` operators on raw strings.

**No evidence found** of spreadsheet users writing `< "D50"` or `>= "C00"` on ICD-10 codes in formulas.

**Citation:** MSDN/Microsoft Learn archived forum "ICD-10 Code Ranges for Diagnoses" (2017), https://learn.microsoft.com/en-us/archive/msdn-technet-forums/923084da-9baf-44c0-857e-951926f51a54 — shows practitioners using `LIKE 'A1[5-9]%'` and `PATINDEX` for tuberculosis code matching (A15-A19), not string range operators.

---

### 2. No-Code Platforms (Airtable, AppSheet)

**Result: No string range comparison on code fields found; dominant approaches are lookup tables and exact-match membership.**

Airtable supports `<`, `>`, `<=`, `>=` operators in its formula language, but all documented and community-discussed examples apply these to numeric fields. The official docs say "Greater than — if the comparison is true, the result will be a 1" in the context of numeric comparisons only. No community forum thread asking about ZIP code service areas or diagnosis code ranges in Airtable uses range comparison operators on string fields. Both threads investigated (ZIP code lookup, ZIP code pricing) reached the same conclusion: use lookup tables and exact matching.

AppSheet's community forum thread on ZIP code matching used `CONTAINS()` over a concatenated string of ZIP codes — not range operators.

The prior research ([`business-string-ordering-use-cases.md`](./business-string-ordering-use-cases.md)) surveyed Airtable in detail and confirmed: the formula language does not provide built-in string ordering, and the community uses `SWITCH` and exact membership for classification.

**No evidence found** of no-code platform users writing string range comparisons on code fields.

---

### 3. Business Rules Engines: Drools (DRL)

**Result: Drools explicitly supports `<` on String fields, but no healthcare examples using this on code ranges were found in the wild.**

Drools DRL documentation explicitly states: "For `String` fields, the `<` operator means _alphabetically before_." The documentation example is `persons[ firstName < $otherFirstName ]` — comparing names. The capability exists in the engine.

However, despite targeted searches for healthcare examples using `diagnosisCode < "D50"` or equivalent Drools rules on ICD-10 codes, **no real-world examples were found** in public documentation, tutorials, blog posts, or forum threads. Healthcare Drools examples in the wild use:
- Equality checks (`diagnosisCode == "B20"`)
- Java method calls (`diagnosisCode.startsWith("A1")`)
- Pattern matching via Java's `matches()` regex

The GoRules insurance prior authorization template for Drools uses code equality patterns, not range comparisons.

**Finding:** Drools supports string ordering syntactically but no documented real-world healthcare prior-authorization or claims-processing rule uses `<` or `>` on ICD-10 or CPT code strings.

---

### 4. Business Rules Engines: FEEL/DMN (Camunda, Red Hat Decision Manager)

**Result: FEEL's support for string range comparison is ambiguous by spec and broken in practice for multi-character strings.**

The OMG DMN specification states that "comparable values" for range endpoints include "numbers, dates, or text (lexicographic order)." Strings are listed as comparable, implying `<` and `>` should work. The CQL (HL7 Clinical Quality Language) specification is explicit: "The LessOrEqual operator is defined for the Integer, Long, Decimal, **String**, Date, DateTime, Time, and Quantity types. String comparisons are strictly lexical based on the Unicode value of the individual characters in the string."

However, practical reality diverges:

- **Camunda's legacy FEEL documentation** (7.4 series): "Strings in FEEL support only the equal comparison operator." String ranges are explicitly excluded from the Camunda DMN hit policy table feature.
- **GitHub issue #73 in the `feelin` library** (Camunda's JavaScript FEEL engine, 2024, open/unresolved): a user reports that `x in ["1015CJ".."1020ZZ"]` fails with "illegal range start: 1015CJ" — the engine rejects multi-character string range notation. Single-character ranges like `x in ["A".."C"]` work. The reporter notes that Camunda FEEL-Scala supports the feature but the JavaScript engine does not.
- **Red Hat Decision Manager documentation** restricts range comparisons to numeric and date types.

The HL7 CQL specification does support string ordering by spec (Unicode lexical), but the CQL author's guide shows no examples of string comparison applied to diagnosis codes. CQL code ranges are expressed exclusively through **ValueSet membership** — a code set defined externally, not a range expression.

**Finding:** FEEL/DMN string range comparisons are theoretically permitted by the OMG spec but broken or disabled in major implementations (Camunda legacy FEEL). Even where the spec allows them, practitioners use ValueSet membership for code range matching, not string `<`/`>` operators. No real-world FEEL rule using `diagnosisCode < "D50"` was found.

**Citation:** GitHub issue https://github.com/nikku/feelin/issues/73 — direct evidence that multi-character string ranges are a known bug in the FEEL ecosystem.

---

### 5. Healthcare-Specific Systems (Payer Policies, Clinical Decision Support)

**Result: Code ranges in clinical policies are expressed as explicit membership lists or ValueSets, not string range comparisons.**

**EmblemHealth prior authorization policy** (fetched, October 2024): The policy specifies that certain CPT/HCPCS codes require prior auth "when paired with diagnoses falling within the C81.00-C88.91 range." In the descriptive text, this is written as a range (C81.00 through C88.91). In the technical implementation, the document provides a **complete itemized list** of every applicable ICD-10 code — approximately 50+ individual codes. The policy describes ranges in human-readable form but implements them as exhaustive membership lists.

**HL7 CQL / FHIR**: Clinical Quality Language, the standard used in FHIR-based quality measures and clinical decision support, handles code ranges exclusively through **ValueSets** — curated, versioned code sets maintained by standards bodies (NLM VSAC, CMS). A CQL expression like `[Condition: "Neoplasms"]` retrieves conditions whose codes are members of the "Neoplasms" ValueSet. There is no `diagnosisCode >= "C00" and diagnosisCode < "D50"` pattern in any published CQL measure.

**Pega Foundation for Healthcare**: Implements ICD-10, HCPCS, and CPT code versioning via uploaded code tables (CMS-published files imported as structured data). Rule matching is against these tables, not against string range operators.

**InterQual / MCG / Milliman**: Clinical criteria tools used in prior authorization. These systems maintain curated code sets per clinical criterion, not string comparison expressions.

**Arden Syntax / GELLO**: The historical clinical decision support standards. GELLO (Object-Oriented Expression Language, HL7) provides string operators but academic papers reference code matching against ICD-10 vocabularies using wildcard/truncation (`K29*` matches all gastritis subcodes), not string ordering operators.

**Finding:** The healthcare IT ecosystem's standard for code range matching is **code set membership** (ValueSet, code table, curated list), not string comparison operators. Ranges appear in human-readable clinical policy documents but are implemented as explicit code lists or ValueSet membership in technical systems.

---

### 6. SQL in Healthcare IT

**Result: SQL BETWEEN exists and is technically used, but practitioners in healthcare IT contexts primarily use LIKE/PATINDEX for ICD-10 range queries.**

SQL does support `WHERE diagnosis_code BETWEEN 'C00' AND 'D49'`, and SQL string comparison is collation-based. However, the evidence from real-world healthcare SQL queries (MSDN forum, OpenEMR community) shows practitioners reaching for LIKE and PATINDEX rather than BETWEEN for ICD-10 code ranges. The MSDN thread shows a healthcare data analyst using:

```sql
IIF([DXCode] LIKE 'A1[5-9]%', 1, 0) [Tuberculosis]
```

and the accepted answer recommends `PATINDEX` with a regex-like code table pattern, not BETWEEN.

The recommendation to normalize the data (one code per row) before range queries was given, which is the clean approach. The BETWEEN operator could be applied to normalized string data, but the evidence suggests practitioners often prefer LIKE with character class patterns over BETWEEN for ICD-10.

The Mark's SQL blog post ("Searching ICD-10 Code Ranges") was inaccessible (domain redirect), but appeared in multiple search result summaries as using BETWEEN with ICD-9 GEM data (`DAD.Icd09Code BETWEEN '812.00' AND '813.93'`) — suggesting string BETWEEN does appear in healthcare SQL contexts, though the accessible evidence shows LIKE as the dominant pattern.

**Finding:** SQL BETWEEN on string diagnosis codes exists (at least for ICD-9 numeric-style codes), but is not the dominant approach for ICD-10 in accessible community examples. LIKE and pattern matching are more common. No evidence of `>=`/`<=` operators on ICD-10 strings outside of SQL.

---

### 7. Summary Across Platforms

| Platform | String `<`/`>` on code fields? | Dominant real-world approach |
|---|---|---|
| Excel / Google Sheets | Technically supported, no evidence of use on codes | VLOOKUP exact match, LIKE patterns, LEFT/MID extraction |
| Airtable / AppSheet | No evidence of use; no-code platforms avoid this | Lookup tables, exact membership |
| Drools / DRL | Syntactically supported; no healthcare examples found | Equality, Java method calls, pattern match |
| FEEL / DMN | Theoretically permitted by OMG spec; broken in Camunda for multi-char strings | ValueSet membership; function wrapping `lower case(x) < lower case(y)` |
| HL7 CQL | Spec-level support (Unicode lexical); no examples on code fields | ValueSet membership exclusively |
| Healthcare payer policies | No technical use of string range operators | Explicit code lists, ValueSets, code tables |
| Healthcare SQL | BETWEEN seen in ICD-9 context; LIKE dominant for ICD-10 | LIKE patterns with character classes, PATINDEX |

---

## Implications for Precept

### The gap analysis was directionally correct but empirically unverified

The gap analysis ([`string-ordering-gap-analysis.md`](./string-ordering-gap-analysis.md)) correctly identified that fixed-format standardized code systems exist, that ordered choice cannot model them, and that ordinal comparison would give correct results on these fields. However, this survey finds **no evidence that real practitioners actually write range rules using string comparison operators on these fields**. The pattern the gap analysis hypothesized (`diagnosisCode >= "C00" and diagnosisCode < "D50"`) exists nowhere in the accessible external evidence.

### The real-world solution is code set membership, not string comparison

The consistent finding across all healthcare-oriented systems surveyed is that practitioners implement code range logic through **code set membership** — maintaining a list of qualifying codes (as a ValueSet, a lookup table, or an explicit enumeration) and testing whether a code is a member. This is the approach used by:
- HL7 CQL / FHIR ValueSets
- Prior authorization payer policies (explicit code lists)
- Pega Foundation for Healthcare (code table imports)
- EmblemHealth clinical policies (50+ individual codes listed)
- Healthcare SQL practitioners (LIKE with character class patterns)

This approach is unambiguous, survives code system updates correctly (a code added to ICD-10 in the next annual update is explicitly included or excluded), and requires no ordinal-correctness assumption.

### String range comparison is a power-user / developer tool, not a business-user pattern

The only contexts where string `<`/`>` appears on code fields are:
1. SQL databases (technical, developer-facing, using collation-based comparison)
2. Drools DRL rules (technical, developer-facing Java-based engine)

Neither of these is the authoring surface that Precept targets. Precept's primary author is a domain expert, not a developer. No evidence was found of domain experts in business-rule authoring tools using string range operators on code fields.

### The `startsWith` alternative is more common than string range in practice

In every accessible real-world example of ICD-10 range matching that avoided explicit code lists, practitioners used:
- `LIKE 'C%'` (prefix match)
- `LIKE 'A1[5-9]%'` (character class pattern)
- LEFT(code, 1) extractions

These correspond to Precept's `startsWith()` function, which already exists. The gap analysis argued that `startsWith` fails for sub-chapter ranges that don't align with a character prefix (e.g., "C50-C58 breast malignancies" vs. `startsWith("C5")` which overcaptures C59). This is a real correctness argument, but **no evidence was found that practitioners encounter this problem and solve it with string range operators** — they either use explicit code lists, or they accept the coarser prefix approximation.

---

## Conclusions

### Conclusion 1: The empirical case for implementing string ordering on code fields is weak

**Rationale:** This survey found zero real-world examples of business users or domain experts writing `<`/`>`/`<=`/`>=` on ICD-10, ZIP, CPT, or NAICS code fields in any no-code platform, business rules engine (at the user/analyst authoring level), spreadsheet, or payer policy system. The pattern exists in SQL (technical/developer layer) but not at DSL authoring surfaces. Precept's primary author is a domain expert, not a SQL developer.

**Alternatives considered and rejected:**
- "Absence of evidence is not evidence of absence." True, but the investigation was broad (7 platform categories, multiple real forum threads and policy documents) and specifically targeted the use case. The absence of any example — including in systems that technically support the feature (Drools, CQL) — is meaningful.
- "Maybe practitioners use it but don't post about it." Possible, but Precept makes decisions based on evidence, not speculation. The burden is on evidence of demand, not absence of contradiction.

**Precedent:** Every healthcare IT system surveyed that handles code range matching uses code set membership (ValueSet, lookup table, explicit enumeration) as the primary mechanism. This is the industry standard.

**Tradeoff accepted:** If string ordering is not added, a Precept author who wants to express "this diagnosis code is in chapter 2 of ICD-10" must either enumerate codes, use `startsWith("C") or startsWith("D")`, or maintain an external code set. These are all current workarounds in the industry and are the standard approach.

---

### Conclusion 2: The prior research was correct that ordered choice covers the main discrete-rank use case

The prior research in [`string-ordering-vs-ordered-choice.md`](./string-ordering-vs-ordered-choice.md) and [`string-ordering-architectural-analysis.md`](./string-ordering-architectural-analysis.md) was right that ordered choice handles the primary practical need for string ordering. The gap analysis identified a real theoretical gap, but this survey finds no confirmed evidence that the theoretical gap translates into a real authoring need.

---

### Conclusion 3: The gap analysis recommendation to implement string ordering requires revision

The gap analysis concluded: "Do not remove string ordering from the language spec. Implement it." This survey does not find external evidence supporting that recommendation. The gap analysis's internal logic is correct (ordinal comparison IS semantically valid for fixed-format codes), but internal logic is not sufficient for implementation — demonstrated demand is required.

**Revised recommendation:** The `string-ordering-gap-analysis.md` conclusions should be revisited. The gap analysis established a theoretical case; this survey tests it against external reality and finds the demand signal absent. The appropriate outcome is: **do not implement string ordering proactively on the basis of the gap analysis alone**. If a Precept author demonstrates a real need to express code range rules that `startsWith` cannot handle, that constitutes concrete demand. The gap analysis can serve as grounding for a future proposal at that point.

This does NOT invalidate the prior research's documentation-only fix verdict: the spec should be corrected to match the implementation (string ordering is not implemented), per the F-LANG-PRIM-01 finding.

---

## Open Questions

1. **The Mark's SQL blog post** ("Searching ICD-10 Code Ranges") was inaccessible due to domain redirect. This post appeared in multiple search results as potentially showing BETWEEN on ICD-10 code strings. If accessible, it might provide the first clear example of a practitioner using string range comparison on ICD-10 codes in SQL outside of ICD-9 GEM data.

2. **IBM ODM, FICO Blaze Advisor, Pegasystems** — the proprietary commercial healthcare rules engines used by major payers (UnitedHealth, Aetna, etc.) were not accessible for this survey. These systems may have their own rule authoring surfaces that expose string range comparison. The evidence suggests they use code set membership, but this is indirect.

3. **FHIR Clinical Reasoning resources** — do any published CQL quality measures or FHIR CPG resources contain string `<`/`>` on code fields? A targeted search of the eCQI resource center or the HL7 FHIR quality measure IG might turn up examples.

4. **The feelin bug** (GitHub issue #73) is unresolved. If FEEL string ranges are supposed to work by spec but are broken in the dominant JavaScript implementation, this represents a latent demand that practitioners are blocked from satisfying, not a demand that doesn't exist. A follow-up on whether this issue gets resolved — and whether practitioners start using it — would be informative.

5. **What do Precept corpus authors actually want?** User research (even a single interview with a healthcare domain expert who has used Precept's ICD-10 fields) would provide more signal than any amount of external platform research. The theoretical case for string ordering on code fields is sound; what's missing is evidence of demand.

---

## Sources

- Microsoft Learn (archived MSDN forum): "ICD-10 Code Ranges for Diagnoses" (2017) — https://learn.microsoft.com/en-us/archive/msdn-technet-forums/923084da-9baf-44c0-857e-951926f51a54
- EmblemHealth: Preauthorization requirement changes starting October 2024 — https://www.emblemhealth.com/providers/clinical-corner/um-and-medical-management/pre-authorization-list/preauthorization-requirement-changes-starting-october-202411
- Drools Rule Language Reference — https://docs.drools.org/8.39.0.Final/drools-docs/docs-website/drools/language-reference/index.html
- Drools DMN FEEL Handbook — https://kiegroup.github.io/dmn-feel-handbook/
- Camunda FEEL language elements (DMN 1.1 legacy) — https://docs.camunda.org/manual/7.4/reference/dmn11/feel/language-elements/
- GitHub issue: `feelin` string range comparison bug — https://github.com/nikku/feelin/issues/73
- HL7 CQL Reference (v1.5.3): LessOrEqual, string comparison semantics — https://cql.hl7.org/09-b-cqlreference.html
- HL7 CQL Author's Guide: ValueSet usage for code ranges — https://cql.hl7.org/02-authorsguide.html
- Microsoft DAX operator reference — https://learn.microsoft.com/en-us/dax/dax-operator-reference
- Airtable: "Comparing Text Strings Using IF Statements" — https://support.airtable.com/docs/comparing-text-strings-using-if-statements
- Airtable community: "Zip Code Lookup table?" — https://community.airtable.com/t5/formulas/zip-code-lookup-table/td-p/67202
- AppSheet community: ZIP code CONTAINS approach — https://community.appsheet.com/t/how-do-i-use-contain-expression-trying-to-find-certain-zip-codes/18176
- BMC Medical Research Methodology: Excel-based ICD-10 comorbidity calculators — https://bmcmedresmethodol.biomedcentral.com/articles/10.1186/s12874-021-01492-7
- John D. Cook: Regex pattern for ICD-10 codes (regex approach, not range operators) — https://www.johndcook.com/blog/2019/05/05/regex_icd_codes/
- Mark's SQL Development: "Searching ICD-10 Code Ranges" (inaccessible — domain redirect) — https://www.marksqldev.com/2018/05/searching-icd-10-code-ranges.html
- Power BI / Microsoft Fabric community: ICD code filtering in DAX (SEARCH function, not range operators) — https://community.fabric.microsoft.com/t5/DAX-Commands-and-Tips/Filter-text-values-in-table-A-based-on-partial-match-from-table/m-p/813559
- Prior research: [`string-ordering-gap-analysis.md`](./string-ordering-gap-analysis.md)
- Prior research: [`string-ordering-vs-ordered-choice.md`](./string-ordering-vs-ordered-choice.md)
- Prior research: [`string-ordering-architectural-analysis.md`](./string-ordering-architectural-analysis.md)
- Prior research: [`business-string-ordering-use-cases.md`](./business-string-ordering-use-cases.md)
