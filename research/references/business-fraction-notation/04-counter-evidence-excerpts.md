# Mirrored excerpts — counter-evidence sweep (fraction notation rare/declining/prohibited)

> Companion to the findings fragment produced for the adversarial counter-evidence sweep on business/
> finance/legal fraction notation. Raw source mirrors (HTML/PDF) live alongside this file in this same
> directory. Access date for all excerpts below: 2026-07-20.

## SEC Release No. 34-42914 (June 8, 2000) — decimal phase-in order

Mirror: `sec-release-34-42914-order.htm`

> "On January 28, 2000, the Commission issued an Order requiring the Participants to facilitate an
> orderly transition to decimal pricing in the United States securities markets. The Order prescribed a
> timetable for the Participants to begin trading some equity securities, and options on those equity
> securities, in decimals by July 3, 2000, and all equities and options by January 3, 2001."

> "[A pilot] would, among other things, minimize the difficulties faced by the securities industry to
> create and maintain separate processes, systems, programs, and procedures for both decimals and
> fractions and would simplify the educational effort directed at the investing public to assist them in
> understanding how specific securities would be priced."

Title of the order itself (primary evidence of scope — equities AND options): "Order Directing the
Exchanges and the National Association of Securities Dealers, Inc. to Submit a Phase-in Plan to Implement
Decimal Pricing in Equity Securities and Options; Pursuant to Section 11A(a)(3)(B) of the Securities
Exchange Act of 1934."

Source grade: Primary (SEC rulemaking release). URL:
https://www.sec.gov/rules-regulations/2000/06/order-directing-exchanges-national-association-securities-dealers-inc-submit-phase-plan-implement

## SEC Chairman Arthur Levitt, congressional testimony (June 13, 2000)

Mirror: `sec-levitt-testimony-2000-06-13.htm`

> "The convention of quoting stock prices in fractions dates back more than two hundred years. Currently,
> the United States securities markets are the only major markets not to price stocks in decimals. As the
> securities markets become more global, with many stocks traded in multiple jurisdictions, the U.S.
> securities markets need to adopt the international convention of decimal pricing to remain competitive.
> And the overall benefits of decimal pricing are likely to be significant. Investors may benefit from
> lower transaction costs due to narrower spreads. Moreover, the markets will be easier to understand for
> the average investor, who is used to dealing in dollars and cents for every-day transactions."

> "Under the order, all securities must be priced in decimals no later than April 9, 2001 -- shortly after
> the end of the quarter."

Source grade: Primary (official congressional testimony by the sitting SEC Chairman). URL:
https://www.sec.gov/news/testimony/ts092000.htm

## SEC Press Release 2024-137 (Sept. 18, 2024) — Rule 612 half-cent tick amendment

Mirror: `sec-2024-137-press.htm`

> "The amendments to Rule 612 establish a new, additional $0.005 minimum pricing increment for
> quotations and orders in NMS stocks that are priced at, or greater than, $1.00 per share. The tick size
> for all NMS stocks will be based on the Time Weighted Average Quoted Spread for the relevant NMS stock
> during a specified three-month Evaluation Period and thereafter assigned for a six-month period."

Source grade: Primary (SEC press release announcing adopted rule amendment). URL:
https://www.sec.gov/newsroom/press-releases/2024-137

## SEC EDGAR XBRL Guide (Prepared by SEC Staff, March 2024) — fraction item type prohibition

Mirrors: `sec-xbrl-guide.pdf`, `sec-edgar-filer-manual-vol2.pdf`

> "Element xs:element attribute type value is not i:fractionItemType" [validation check] →
> failure name: "Fraction Item Type" — severity Error — EFM v68 Ref § 6.7.31.

> "EDGAR standard taxonomies do not define fraction item types nor custom arc roles."

Source grade: Primary (SEC staff technical guide governing EDGAR XBRL submission validity). URL:
https://www.sec.gov/files/edgar/filer-information/specifications/xbrl-guide-2024-03-12.pdf

## XBRL US practitioner guidance — corroborating the 2014 SEC prohibition

> "use the fraction item type for this purpose, which was prohibited from use in 2014 by the SEC"

Source grade: Secondary (XBRL US is the jurisdiction body for US XBRL practice, not the SEC itself). URL:
https://xbrl.us/guidance/handling-values-expressed-as-fractions/ (not independently mirrored as a file —
short guidance page, quote captured directly).

## NIST Special Publication 1038 — "Note on Mixed Units and Fractions"

Full text fetched and grepped locally (see fragment for the correction of an earlier SP 811
misattribution); not re-saved as a permanent mirror file in this pass because of size, but the exact
passage and location are recorded here for reproducibility:

> "Note on Mixed Units and Fractions. Mixed units, which are commonly used with inch-pound units, are not
> used in metric practice. Thus, while a distance may be given in inch-pound units as 27 feet 5 inches,
> metric practice shows a length as 3.45 m rather than 3 m, 45 cm. Binary fractions (such as 1/2 or 3/8)
> are not used with metric units. For example, a person's weight is given as 70.5 kg, not 70-1/2 kg."

Location: NIST Special Publication 1038, "The International System of Units (SI) – Conversion Factors for
General Use" (authors: Kenneth Butcher, Linda Crown, Elizabeth J. Gentry; NIST Weights and Measures
Division), § 5 "Detailed Requirements and Conversion Factors," subsection "Note on Mixed Units and
Fractions."

Source grade: Primary (official NIST special publication). URL:
https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication1038.pdf

**Explicit non-finding, verified**: NIST Special Publication 811 (2008 edition, "Guide for the Use of the
International System of Units (SI)") was separately fetched in full and searched for "fraction" and
"mixed unit" — it does **not** contain the above passage or any equivalent binary-fraction prohibition.
Its only uses of "fraction" concern dimension-one physical-chemistry ratio quantities (mass fraction, mole
fraction, volume fraction), e.g.: "the mass fraction is 0.10," or "wB = 0.10" — a different sense of the
word entirely. URL checked: https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication811e2008.pdf

## 16 CFR § 500.17 — Fair Packaging and Labeling Act regulations, "Fractions"

Mirror: `fpla-16cfr500.htm`

> "§ 500.17 Fractions. (a) SI metric declarations of net quantity of contents of any consumer commodity
> may contain only decimal fractions. Other declarations of net quantity of contents may contain common
> or decimal fractions. A common fraction shall be in terms of halves, quarters, eighths, sixteenths, or
> thirty-seconds; except that: (1) If there exists a firmly established general consumer usage and trade
> custom of employing different common fractions in the net quantity declaration of a particular
> commodity, they may be employed, and (2) If linear measurements are required in terms of yards or feet,
> common fractions may be in terms of thirds. A common fraction shall be reduced to its lowest terms; a
> decimal fraction shall not be carried out to more than three places."

Compliant labeling example given elsewhere in the same regulation showing common-fraction notation is
affirmatively legal for inch-pound declarations:

> "(Examples: ... or 'Net Wt. 5 1/4 lbs. (2.38 kg)' or 'Net Mass 2.38 kg (5 1/4 lbs.)' ...)"

Source grade: Primary (Code of Federal Regulations text). URL:
https://www.govinfo.gov/content/pkg/CFR-2011-title16-vol1/xml/CFR-2011-title16-vol1-part500.xml

**Read this narrowly**: this regulation restricts fraction notation to decimal-only *only* for SI/metric
declarations. For the inch-pound units that dominate US consumer packaging, it explicitly authorizes p/q
common fractions by name and denominator family (halves/quarters/eighths/sixteenths/thirty-seconds). This
is evidence *against* the "fractions are broadly prohibited" thesis in the specific case of US package
labeling law, even while confirming the SI-only restriction the adversarial brief was looking for.

## BIPM SI Brochure, 9th edition — checked, no fraction-notation prohibition found

Full PDF fetched and grepped for "fraction." No style-guide instruction to prefer decimal over p/q
fraction notation for ordinary numeral values was found anywhere in the document. All occurrences of
"fraction" are either dimension-one ratio quantities (same pattern as NIST SP 811) or historical CGPM
resolution text defining base units (e.g., "the kelvin ... is the fraction 1/273.16 of the thermodynamic
temperature of the triple point of water"). Recorded here as a checked-and-rejected line, not a citation.
URL checked: https://www.bipm.org/documents/20126/41483022/SI-Brochure-9-EN.pdf

## Liberty Street Economics (Federal Reserve Bank of New York) — Treasury securities still fractional

> "Unlike U.S. equity markets, which switched to decimal pricing in 2001, U.S. Treasury securities still
> trade in fractions... prices are quoted in 32nds of a point, where a point equals one percent of par,
> with the 32nds themselves split into fractions... 3- and 5-year notes trade in quarters of 32nds,
> whereas 7-, 10-, and 30-year securities trade in halves of 32nds."

Source grade: Secondary (Federal Reserve Bank of New York research blog). URL:
https://libertystreeteconomics.newyorkfed.org/2020/01/how-does-tick-size-affect-treasury-market-quality/

This is a negative result for the adversarial sweep (Treasuries did not decimalize) and is recorded here
because it directly bounds the scope of the equities-decimalization finding above.

## ISO 80000-1:2022 — could not verify (paywalled)

Only a 12-page preview sample was accessible; it contains no occurrence of "fraction." No claim rests on
this standard in the fragment. URL checked:
https://cdn.standards.iteh.ai/samples/76921/7dc5d3e9e92a4c94b3885e113b65cd3d/ISO-80000-1-2022.pdf
