# Source excerpts — authoring-tool fraction notation (notation vs. display vs. storage)

Mirror of load-bearing verbatim excerpts backing
`research/language/rational-vs-decimal-exact-numeric-representation.md` fragment "02-authoring-tools".
Fetched 2026-07-20. Grading convention for this file: Primary = official standard/spec, OR a vendor's
own reference/help documentation for its own product; Secondary = named-author third-party articles or
vendor-hosted community posts; Tertiary = forum posts / unverified synthesis / background knowledge not
tied to a fetched source. (This departs from the repo-wide `research/` SKILL.md default of grading all
vendor docs as Secondary — see the parent fragment's Threats to Validity for why.)

---

## Microsoft Excel

**Fraction format Type options** [Secondary/vendor; accessed 2026-07-20;
https://support.microsoft.com/en-us/office/display-numbers-as-fractions-0121ecac-1773-4f2d-8cd3-7db51fd83b77]:

> "Fraction format | This format displays 123.456 as | Single-digit fraction | 123 1/2, rounding to the
> nearest single-digit fraction value | Double-digit fraction | 123 26/57, rounding to the nearest
> double-digit fraction value | Triple-digit fraction | 123 57/125, rounding to the nearest triple-digit
> fraction value | Fraction as halves | 123 1/2 | Fraction as quarters | 123 2/4 | Fraction as eighths |
> 123 4/8 | Fraction as sixteenths | 123 7/16 | Fraction as tenths | 123 5/10 | Fraction as hundredths |
> 123 46/100"
> — "Display numbers as fractions", Microsoft Support

> "If you enter a fraction and Excel thinks you want a date, enter a zero and a space before the fraction
> (e.g., `0 9/12`). The zero will disappear after pressing Enter, and the cell will display the fraction."
> — same page

**Date auto-conversion and the `0 1/2` workaround** [Secondary/vendor; accessed 2026-07-20;
https://support.microsoft.com/en-us/office/stop-automatically-changing-numbers-to-dates-452bd2db-cc96-47d1-81e4-72cec11c4ed8]:

> "A zero and a space before you enter a fraction such as 1/2 or 3/4 so that they don't change to 2-Jan
> or 4-Mar, for example. Type 0 1/2 or 0 3/4."
> — "Stop automatically changing numbers to dates", Microsoft Support

**Underlying storage — IEEE 754 double-precision, with 1/3 as the worked example** [Primary/vendor;
accessed 2026-07-20;
https://learn.microsoft.com/en-us/troubleshoot/microsoft-365-apps/excel/floating-point-arithmetic-inaccurate-result]:

> "Microsoft Excel was designed around the IEEE 754 specification to determine how it stores and
> calculates floating-point numbers... The 754 standard is used in the floating-point units and numeric
> data processors of nearly all of today's PC-based microprocessors."

> "In the case of Excel, although Excel can store numbers from 1.79769313486232E308 to
> 2.2250738585072E-308, it can only do so within 15 digits of precision. This limitation is a direct
> result of strictly following the IEEE 754 specification and isn't a limitation of Excel."

> "The bias for single-precision numbers is 127 and 1,023 (decimal) for double-precision numbers. Excel
> stores numbers using double-precision."

> "Even common decimal fractions, such as decimal 0.0001, can't be represented exactly in binary. (0.0001
> is a repeating binary fraction that has a period of 104 bits). This is similar to why the fraction 1/3
> can't be exactly represented in decimal (a repeating 0.33333333333333333333)."
> — "Floating-point arithmetic may give inaccurate result in Excel", Microsoft Learn / Microsoft 365 Apps
> troubleshooting docs

---

## Google Sheets

**Fraction custom-format token** [Secondary/vendor; accessed 2026-07-20;
https://support.google.com/docs/answer/56470?hl=en]:

> "/" — "Formats numbers as a fraction."
> — "Format numbers in a spreadsheet", Google Docs Editors Help

**`0 1/2` leading-zero entry convention** [Secondary/community synthesis, not independently verified from
a Google primary doc; accessed 2026-07-20; search-result synthesis over
https://infoinspired.com/google-docs/spreadsheet/format-numbers-as-fractions-in-google-sheets/ (full page
fetch returned HTTP 403) and related community threads]:

> "When you type 0 1/2, Google Sheets recognizes it as the numerical value 0.5 but displays it as '1/2'
> with the correct fraction format already applied... your fractions can be used in formulas without any
> extra work, as Google Sheets will simply use their underlying decimal value for the calculation."
> — search-engine synthesis over community/third-party sources; flagged Tertiary/Secondary blend, not a
> direct page fetch

---

## DMN / FEEL — OMG Decision Model and Notation specification v1.3

Source: OMG DMN 1.3 specification PDF, fetched and converted to text via `pdftotext -layout`
[Primary/spec; accessed 2026-07-20; https://www.omg.org/spec/DMN/1.3/PDF].

**FEEL number type semantics (§10.3.2.3.1 "number")**:

> "FEEL Numbers are based on IEEE 754-2008 Decimal128 format, with 34 decimal digits of precision and
> rounding toward the nearest neighbor with ties favoring the even neighbor. Numbers are a restriction of
> the XML Schema type precisionDecimal, and are equivalent to Java BigDecimal with MathContext DECIMAL
> 128."

> "Grammar rule 35 defines literal numbers. Literals consist of base 10 digits and an optional decimal
> point. –INF, +INF, and NaN literals are not supported. There is no distinction between -0 and 0."

> "FEEL does not support a literal scientific notation. E.g., 1.2e3 is not valid FEEL syntax. Use
> 1.2*10**3 instead."

> "A FEEL number is represented in the semantic domain as a pair of integers (p,s) such that p is a
> signed 34 digit integer carrying the precision information, and s is the scale, in the range
> [−6111..6176]. Each such pair represents the number p/10^s."

> "There is no value for notANumber, positiveInfinity, or negativeInfinity. Use null instead."
> — DMN 1.3 spec, § 10.3.2.3.1

**Numeric literal grammar (rule 35) and division grammar (rule 22) — separate productions**:

> "35. numeric literal = [ "-" ] , ( digits , [ ".", digits ] | "." , digits ) ;"

> "22. division = expression , "/" , expression ;"
> — DMN 1.3 spec, Annex B grammar rules (line numbers per local `pdftotext -layout` extraction: rules 35
> and 22 respectively; note the spec's own numbering differs slightly between the semantics narrative,
> which cites rule 35 for numeric literal, and the grammar Annex, which also lists rule 22 for division —
> both productions confirmed present and structurally distinct in the extracted text)

**FEEL number/comparison background reference to IEEE 754**:

> "IEEE 754-2008, IEEE Standard for Floating-Point Arithmetic, International Electrical and Electronics
> [Engineers]"
> — DMN 1.3 spec, normative references section

**Drools' own FEEL handbook (implementation-level, not the OMG spec itself)** [Secondary/vendor; accessed
2026-07-20; https://kiegroup.github.io/dmn-feel-handbook/]:

> "Numbers in FEEL are based on the IEEE 754-2008 Decimal 128 format, with 34 digits of precision."

> "Internally, numbers are represented in Java as BigDecimals with MathContext DECIMAL128."

> "FEEL supports only one number data type, so the same type is used to represent both integers and
> floating point numbers."

> "FEEL numbers use a dot (.) as a decimal separator."

> The handbook's only appearance of `1/3`-shaped notation is inside a function call example,
> `decimal( 1/3, 2 ) = .33` — division used as a computed expression argument to the `decimal()` rounding
> function, not as a standalone fraction literal.

---

## Drools DRL (rule language, distinct from DMN/FEEL)

[Secondary/vendor; accessed 2026-07-20;
https://docs.drools.org/8.39.0.Final/drools-docs/docs-website/drools/language-reference/index.html]

> Division functions as an arithmetic operator within constraint expressions, e.g.:
> `"Math.round( weight / ( height * height ) ) < 25.0"`

> The grammar's railroad diagrams define `IntLiteral`, `RealLiteral`, `RealTypeSuffix`, `Fraction`, and
> `Exponent` components for numeric literals — `Fraction` here denotes the decimal-point fractional part
> of a `RealLiteral` production (i.e. the digits after the decimal point in a number like `1.5`), not a
> `numerator/denominator` slash-fraction literal. No documented literal syntax accepts `1/2` as a single
> token; `1 / 2` is division of two `IntLiteral`s.

---

## Power Fx (Microsoft Power Apps / Power Platform)

[Primary/vendor; accessed 2026-07-20; https://learn.microsoft.com/en-us/power-platform/power-fx/data-types]

**Decimal and Float type table entries**:

> "**Decimal** | A number with high precision, base 10 operations, and a limited range. | **123**
> **Decimal( "1.2345" )**"

> "**Float** | A number with standard precision, base 2 operations, and a wide range. | **123**
> **8.903e121** **1.234e-200**"

**Type descriptions**:

> "Power Fx supports two kinds of numbers: **Decimal** and **Float** (with synonyms **Number** and
> **Currency**)."

> "**Decimal** is best for most business calculations. It can accurately represent numbers in base 10
> meaning that `0.1` can be exactly represented and will avoid rounding errors during calculations. It has
> a large enough range for any business need, up to 10^28 with up to 28 digits of precision. **Decimal**
> is the default numeric data type for most Power Fx hosts, used if one simply writes `2*2`."

> "**Float** is best for scientific calculations... Precision is limited to 15 decimal places and math is
> based on base 2 so it can't represent some common decimal values precisely."

**Decimal internal representation**:

> "The **Decimal** data type most often uses the [.NET decimal data type]. Some hosts, such as Dataverse
> formula columns that are run in SQL Server, use the SQL Server decimal data type."

**Float internal representation and worked inexactness example**:

> "The **Float** data type, also known as **Number** or **Currency**, uses the [IEEE 754 double-precision
> floating-point standard]."

> "You might expect the formula **55 / 100 \* 100** to return exactly 55 and **(55 / 100 \* 100) - 55** to
> return exactly zero. However, the latter formula returns 7.1054 x 10^-15, which is very small but not
> zero."

**Literal defaulting to Decimal**:

> "Literal numbers in formulas. The number `1.234` is interpreted as a **Decimal** value. For example, the
> formula `1.234 * 2` interprets the `1.234` and `2` as **Decimal** and return a **Decimal** result."

No fraction-literal (`numerator/denominator` single-token) syntax appears anywhere in the fetched page;
every numeric-literal example shown is decimal-point notation, integer notation, or `Decimal()`/`Float()`
string-conversion calls.

---

## Airtable

[Primary/vendor; accessed 2026-07-20; https://support.airtable.com/docs/number-based-fields-in-airtable]

> "The number field type is a field type designed to hold numbers. The number field type is a
> general-purpose field type for most numerical values, like the number of chairs of a particular type
> your furniture business has in stock, or the distance from one city to another."

> "Numbers with 15+ digits are not recommended for storing in Airtable records because they are
> automatically rounded up."

No text on this page addresses fraction display or fraction-literal entry. A separate community source
[Secondary/community; https://community.airtable.com — synthesized via search, not directly fetched]
states plainly that Airtable "does not have any built-in support for displaying numbers as fractions."

---

## Salesforce

[Primary/vendor; accessed 2026-07-20; https://help.salesforce.com/s/articleView?id=000387302&language=en_US&type=1]

> "The precision of Number values managed through the UI or SObject API is limited by the precision of
> the Java Double type."

> Worked example: entering `49.999999999999972` results in `49.99999999999997` being stored.

> Separately: values entered via the standard UI are rounded/displayed per the field definition "but
> stores the full precision value in the database," while "Apex and API methods can save records with
> decimal places beyond the field definition."

No fraction-literal or fraction-display capability is mentioned anywhere in the fetched content.

---

## Notion (formulas)

[Primary/vendor; accessed 2026-07-20; https://www.notion.com/help/formula-syntax]

> Number property/formula examples shown: `prop("Number") / 2` and `pi() * prop("Radius") ^ 2`.

> Division function documented as `divide(5, 10)` = `0.5`, alongside operator form `5 / 10` = `0.5`.

The `/` character appears exclusively as the division operator in every example on the page; no
fraction-literal notation or fraction-display capability is documented.

---

## NetSuite (SuiteBilling)

[Primary/vendor; accessed 2026-07-20;
https://docs.oracle.com/en/cloud/saas/netsuite/ns-online-help/subsect_1547668485.html]

> "SuiteBilling's Price, Quantity, and Discount fields show up to eight decimal places. Values with more
> than eight decimal places in these fields are rounded to the eighth decimal place."

No fraction notation is mentioned; the doc describes decimal-place precision only, across "modify pricing
change orders, renewal change orders, subscription change orders, Price Book subtabs, price books, price
plans, subscriptions, and Subscription Pricing subtabs."

---

## QuickBooks

[Secondary/community, vendor-hosted; accessed 2026-07-20;
https://quickbooks.intuit.com/learn-support/global/manage-customers-and-income/my-business-buys-in-bulk-repacks-and-sells-the-item-in-smaller/00/1199194]

> Guidance synthesized from the thread: quantity on a sales order can be specified as a decimal such as
> "0.15; 0.25; 0.5" for sub-unit sales — no fraction notation (`1/2`) is shown or referenced as accepted
> input.

---

## SAP MM (material master / unit of measure)

[Tertiary — synthesized from WebSearch snippets over SAP Community posts and third-party sites; the
primary SAP Help Portal page fetch returned only a page header with no body content; accessed 2026-07-20]

> Synthesis (not a direct verbatim page excerpt — SAP Help Portal fetch failed): the SAP quantity domain
> `QUAN` is decimal, commonly defaulting to up to 3 decimal places; separately, unit-of-measure conversion
> factors are configured as a Numerator/Denominator quotient — "Quantity (in alternative unit of measure)
> = quotient * quantity (in base unit of measure)," where "the quotient is expressed as Numerator /
> Denominator." This conversion factor is a system-configured ratio between two units (e.g., how many
> `EA` per `CASE`), not a fraction typed by an end user into a quantity-entry field.

This claim is flagged Tertiary because no primary SAP Help Portal excerpt could be retrieved to confirm
it verbatim; it rests on WebSearch's own synthesis over secondary/community sources.

---

## HotDocs (document assembly)

[Primary/vendor; accessed 2026-07-20;
https://help.hotdocs.com/developer/webhelp/Understanding_Variables_2/uv2_number_variable_formats_percentages_and_fractions.htm]

> "HotDocs does not allow users to directly enter fractions[;] it can convert a decimal value to a
> fraction before it merges the value into the document."

> Users must first ensure "the Decimal places field contains a number greater than 0 so users can enter a
> decimal answer."

> "Example formats can also be used to enter fractions" — "The number in the denominator of the example
> will be the number used in the denominator of the merged value."

> Worked examples: a user answer of `2.5`, formatted with example `9 1/8`, merges as `2 1/2`; the same
> answer formatted with example `9 1/3` merges as `2 2/3`.

> "Fractions are rounded and simplified."

This is the clearest example found in this survey of a tool that explicitly documents its own (a)/(b)/(c)
separation: (a) notation accepted is decimal only, (b) display can be a fraction, generated at merge
time, (c) storage is the decimal answer, with the fraction being a rounded, simplified rendering of it.

---

## AutoCAD (units/quantity-aware surface — Architectural unit format)

**Format code and fractional-precision denominators** [Primary/vendor; accessed 2026-07-20;
https://help.autodesk.com/cloudhelp/2017/ENU/AutoCAD-Core/files/GUID-D396FBFE-6171-4A89-9E68-6CB082EBE0E1.htm]:

> "4 = Architectural 1'-3 1/2""

> "Specifies the fractional precision: 1, 2, 4, 8, 16, 32, 64, 128, or 256."
> — `-UNITS` command reference, Autodesk Help

**Fraction as literal input in Architectural mode** [Secondary/community, corroborating the vendor doc
above; accessed 2026-07-20; search-engine synthesis over Autodesk/CAD community sources]:

> "Architectural units are based in feet and inches and use fractions to represent partial inches: for
> example, 12′3 1/2″. The base unit is the inch unless otherwise specified, so if you enter a number like
> 147.5, then AutoCAD will understand it to be 12′3 1/2″."

**Internal storage type — NOT independently confirmed from a primary Autodesk source in this pass.**
Attempted fetch of `https://blogs.autodesk.com/autocad/working-large-coordinates-in-autocad/` (and its
redirect target `https://www.autodesk.com/blogs/autocad/working-large-coordinates-in-autocad/?redirected=1`)
returned HTTP 403 both times. A WebSearch-synthesized claim states AutoCAD's drawing database holds "14
significant digits" of precision "regardless of the Units Precision setting," consistent with
double-precision floating point, but this is graded **Tertiary** — not a verified vendor excerpt.

---

## Sources not fetchable / gaps

- Contract Express: no vendor documentation on numeric/fraction handling located during this pass.
- Documate: no vendor documentation on numeric/fraction handling located during this pass.
- Tax preparation software (no specific vendor identified/searched successfully): no evidence of fraction
  entry located.
- Actuarial rating software: no evidence of fraction entry located; actuarial ratemaking literature's
  "numerator/denominator" usage (claims count / earned exposures) is a computed-rate concept, not a
  fraction-input UI, and should not be conflated with the notation question.
