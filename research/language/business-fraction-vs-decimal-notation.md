---
status: Active — horizon groundwork
authored: 2026-07-20
author: research sub-agents (commissioned by owner Shane via /research)
topic: how exact non-integer quantities are notated in business, legal, financial and regulatory source material and in domain-expert-facing authoring tools — fraction (p/q) vs decimal numeral, separated from machine storage format
external-engagement: strong
---

# Fraction vs Decimal Notation in Business, Legal, Financial and Regulatory Practice

> What do people in these domains actually **write and read** for an exact non-integer quantity — a
> fraction like `1/3`, or a decimal like `0.333`? This survey deliberately separates **notation**
> (what a human writes), **display** (what a system renders back), and **storage** (what is
> persisted or transmitted). It proposes **no decision** and makes no claim about what Precept
> should do.

## Background

`research/language/rational-vs-decimal-exact-numeric-representation.md` (2026-07-19) surveyed how
*programming languages* natively represent exact non-integer numbers, and established the
storage/interchange answer: finance and commercial computing converged overwhelmingly on **decimal**
(.NET `System.Decimal`, Java `BigDecimal`, IEEE 754-2008 decimal64/128, SQL `NUMERIC`, COBOL packed
decimal, JSON/XBRL/EDI wire formats). That conclusion is **cited here, not re-derived**.

That doc's "Area 6 — Audience fit" paragraph then made a further claim, without any citation:

> "Rational literal syntax (`1//3`, `2/3r`, `n % d`) and fraction display (`Fraction(1, 3)`) are
> Lisp/Haskell/Julia programmer-culture artifacts; decimal notation (`3.14`, `0.1`) is universal
> business notation."

That paragraph is being marked unsupported. This pass exists to establish what the evidence
actually shows about **notation**, in whichever direction it points, because the storage answer does
not automatically transfer to the notation answer. Two live hypotheses were held open throughout:

- **(a)** Domain experts read and write decimals, so fraction notation would be foreign to them.
- **(b)** Fraction notation is native to substantial parts of business and law, and forcing a
  truncated decimal for a non-terminating value is itself an error.

**Horizon-groundwork declaration.** `status: Active — horizon groundwork`. The intended downstream
consumer is the same numeric-lane `/design` pass that
[`rational-vs-decimal-exact-numeric-representation.md`](./rational-vs-decimal-exact-numeric-representation.md)
feeds — specifically its author-facing-notation leg, which that survey could not ground because the
claim it relied on was uncited. This doc supplies the evidence for that leg and nothing more; it
locks no decision and is not policy. Inbound citations exist in both directions: the prior survey
links here from its withdrawn Area 6 paragraph, and this doc cites the prior survey for the
storage/interchange answer it does not re-derive.

## Methodology

- **Research question.** In the source material and authoring surfaces of business, legal,
  financial and regulatory domains, when is an exact non-integer quantity written as a decimal
  numeral, when as a fraction (p/q), and what governs the choice?
- **Search strategy.** Six independent investigations run in parallel, each with its own search
  strategy and source set, then cross-validated:
  1. **Primary legal/financial source material** — statutes, uniform acts, court opinions
     reproducing deeds, standards-body definitions, exchange rulebooks, SEC filings.
  2. **Domain-expert authoring tools** — spreadsheets, DMN/FEEL, rule engines, low-code formula
     languages, ERP entry, document assembly, CAD.
  3. **The exactness problem + comprehension literature** — regulatory rounding mandates,
     legislative drafting manuals, and the empirical fraction-vs-decimal comprehension research.
  4. **Adversarial counter-evidence sweep** — decimalization mandates, standards and style guides
     requiring decimal, data-entry conventions rejecting fractions.
  5. **Fraction-native trades surfaces** — construction calculators, CAD unit modes, lumber
     standards, with a required metric/machining/surveying counter-check.
  6. **Enterprise ERP internal representation** — whether SAP, Oracle and peers store any quantity
     as an integer numerator/denominator pair.
  Venues: EUR-Lex and legislation.gov.uk, SEC.gov and EDGAR, govinfo/eCFR/Cornell LII, IRS.gov,
  federalreserve.gov, TreasuryDirect, delcode.delaware.gov, leginfo.legislature.ca.gov, Uniform Law
  Commission, courtlistener and txcourts.gov, ISDA definitions, CME Group, NIST (nvlpubs), BIPM,
  OMG DMN spec, vendor documentation (Microsoft, Google, Autodesk, Calculated Industries, SAP,
  Oracle), and peer-reviewed journals via DOI.
- **Inclusion / exclusion.** Included: evidence about what humans write and read, and what tools
  accept and display. Excluded (cited, not re-derived): the machine storage/interchange question
  settled by the prior doc. Where a finding is genuinely about storage, it is labeled as such and
  not counted as notation evidence.
- **Source-grade mix.** Predominantly Primary (statutes, regulations, standards bodies, court
  opinions, the OMG DMN specification, NIST publications, SEC releases, peer-reviewed papers).
  Vendor documentation is graded **Secondary** per this repo's convention. A minority of claims are
  Tertiary and are labeled inline. Two agents independently caught and corrected cases where a
  search-tool summary overstated what a primary source said — see Threats to Validity.
- **Time bounds.** All external fetches 2026-07-20. Load-bearing excerpts mirrored to
  `research/references/business-fraction-notation/`.

**A note on grading.** The authoring-tools investigation initially graded vendor documentation as
Primary, on the reasoning that there is no independent spec for (say) Excel's behavior. That
departs from this repo's convention. In this synthesis all vendor documentation is re-graded
**Secondary**; the underlying excerpts are unchanged and remain in the mirror files.

---

## Findings

### 1. The split is real, and it tracks the kind of quantity — not the sophistication of the reader

The single clearest result of this survey is that neither hypothesis (a) nor (b) is right as
stated. Fraction notation and decimal notation are both alive in current, primary business and legal
source material, and which one appears is governed by **what kind of quantity is being expressed**.

| Quantity kind | Dominant notation | Representative primary source |
|---|---|---|
| Intestate succession shares | Word fraction ("one-half", "one-third", "three-fourths") | Uniform Probate Code § 2-102; Cal. Prob. Code § 6401 |
| Deed / mineral & royalty interests | Word fraction **plus** parenthetical numeral: "one sixteenth (1/16)" | Deeds of 1891, 1951, 1977 quoted verbatim in WV and TX opinions |
| Corporate supermajority thresholds | Integer + slash fraction + percent: "66 2/3%" | Del. Code tit. 8 § 203(a)(3) |
| Central-bank rate target ranges | Slash fraction: "1/4 to 1/2 percent" | FOMC Minutes, March 2022 |
| Treasury / bond secondary-market and futures quoting | Fraction (32nds and subdivisions) | CME Group contract specs; NY Fed |
| US retail package net-quantity, non-metric | Common fraction, denominators fixed by regulation | 16 CFR § 500.17 |
| Construction / carpentry linear measure | Binary fraction of an inch | NIST Handbook 44 App. B § 2.4 |
| Unit-of-measure conversion factors (ERP internals) | Integer numerator / denominator pair | SAP `MARM-UMREZ`/`UMREN`; Dynamics AX2012 `Numerator`/`Denominator` |
| — | — | — |
| Currency amounts, invoices, posted ledger entries | Decimal | N.Y. UCC § 3-104; IRS Form 1040 instructions |
| Treasury auction results (issuer's own record) | Decimal, 6 places | TreasuryDirect auction API |
| Convertible-bond conversion ratios | Decimal, 4 places | B2Gold indenture supplement (SEC) |
| LLC / partnership equity splits | Decimal percentage | IDT Holding LLC operating agreement (SEC) |
| Interest rates in swap documentation | Decimal, explicitly | 2006 ISDA Definitions § 5.2(a) |
| Precision machining linear measure | Decimal inch (thousandths) | NIST Handbook 44 App. B § 2.4 |
| Surveying / civil engineering linear measure | Decimal foot | NIST Handbook 44 App. B § 2.4 |

The organizing principle that emerges: **fractions express a share of a whole that is exact by
definition and fixed by agreement; decimals express a measured, priced, or computed magnitude.**

An heir's one-third is not an approximation of 0.333 — the fraction *is* the thing agreed. A bond's
conversion rate of 315.2088 shares per $1,000 is a computed magnitude with a chosen precision. Both
appear in current SEC filings, statutes, and court records; they are not competing conventions for
the same job.

#### The strongest fraction-side evidence

Real-property and mineral conveyance drafting is the cleanest case, and it is verifiably current
rather than historical. The convention — spell the fraction in words, then repeat it as a
parenthetical numeral — is stable across 136 years of instruments and is still being litigated over
verbatim [Primary; access 2026-07-20; mirrored to
`research/references/business-fraction-notation/01-primary-source-excerpts.md`]:

> "The 1891 Deeds, on their face, purported to grant and convey 'the one sixteenth (1/16) part of
> all the oil and gas in and under all the land belonging to the said first parties.' There is no
> reference to 'one-half,' '1/2,' '50%,' or 'fifty percent' in either of the 1891 Deeds."
> — *Country Roads Minerals, LLC v. Goodwin*, W. Va. Intermediate Ct. App., decided 2026-05-01

> "(1) 'an undivided one-half (1/2) interest in and to the Oil Royalty, Gas Royalty and Royalty in
> other Minerals,' (2) 'the same being equal to one-sixteenth (1/16) of the production.'"
> — Texas Supreme Court No. 17-0111 (2018), quoting a 1951 deed

The Uniform Probate Code uses word fractions with no numerals at all in the operative text [Primary]:

> "the first [$150,000], plus one-half of any balance of the intestate estate, if all of the
> decedent's surviving descendants are also descendants of the surviving spouse"
> — Uniform Probate Code § 2-102

And Delaware's anti-takeover statute embeds a fraction inside a percent sign rather than rounding
it — the drafters wrote neither "66.67%" nor "2/3 of the outstanding stock" [Primary]:

> "the affirmative vote of at least 66 2/3% of the outstanding voting stock which is not owned by
> the interested stockholder"
> — Del. Code Ann. tit. 8, § 203(a)(3)

Most striking, because it was found by the investigation that was searching for the *opposite*: a
current US federal regulation not merely tolerating but affirmatively authorizing common fractions,
naming the permitted denominators and requiring reduction to lowest terms [Primary; mirrored to
`04-counter-evidence-excerpts.md` and raw at `fpla-16cfr500.htm`]:

> "§ 500.17 Fractions. (a) SI metric declarations of net quantity of contents of any consumer
> commodity may contain only decimal fractions. Other declarations of net quantity of contents may
> contain common or decimal fractions. A common fraction shall be in terms of halves, quarters,
> eighths, sixteenths, or thirty-seconds ... A common fraction shall be reduced to its lowest terms;
> a decimal fraction shall not be carried out to more than three places."
> — 16 CFR § 500.17, Fair Packaging and Labeling Act regulations

#### The strongest decimal-side evidence

Currency is unambiguous and uncontested. No fraction-of-a-dollar notation appeared anywhere in any
source surveyed. The UCC's negotiability requirement is a "sum certain in money"; the IRS instructs
whole-dollar rounding; posted ledger amounts in every ERP examined are fixed-scale decimals.

A genuine terminology trap is worth flagging because it would otherwise be miscounted as
fraction evidence. The financial term of art **"Day Count Fraction" is not p/q notation.** It names
a formula evaluated against calendar dates [Primary]:

> "if 'Actual/360', 'Act/360' or 'A/360' is specified, the actual number of days in the Calculation
> Period ... divided by 360"
> — 2006 ISDA Definitions § 4.16(e)

and the same document specifies the rate itself as a decimal:

> "'Fixed Rate' means ... a rate, expressed as a decimal, equal to the per annum rate specified"
> — 2006 ISDA Definitions § 5.2(a)

The same trap applies to tax apportionment. California's UDITPA property factor "is a fraction, the
numerator of which is ... and the denominator of which is ..." — the statute names the *shape* of a
computation, and no p/q numeral is ever written down.

### 2. The one place a decimalization mandate genuinely displaced fractions — and exactly how far it reaches

US equity and equity-option price quoting is a real, documented, regulator-forced migration from
fractions to decimals, and the rationale given was partly about human comprehension, not only about
systems [Primary; mirrored to `04-counter-evidence-excerpts.md`, raw at
`sec-levitt-testimony-2000-06-13.htm`]:

> "The convention of quoting stock prices in fractions dates back more than two hundred years.
> Currently, the United States securities markets are the only major markets not to price stocks in
> decimals. ... Moreover, the markets will be easier to understand for the average investor, who is
> used to dealing in dollars and cents for every-day transactions."
> — SEC Chairman Arthur Levitt, congressional testimony, 2000-06-13

Regulation NMS Rule 612 then made sub-penny (and therefore fractional) quoting a rule violation for
stocks at or above $1.00, and the 2024 amendment subdivided the decimal grid further rather than
reopening fractions. Twenty-four years of one-way movement.

**But the reach is narrow, and the same sweep established the limits:**

- The order covered **equities and options only**. Treasuries never decimalized and still quote in
  32nds today [Secondary; Federal Reserve Bank of New York]:
  > "Unlike U.S. equity markets, which switched to decimal pricing in 2001, U.S. Treasury securities
  > still trade in fractions... prices are quoted in 32nds of a point"
- CME grain futures show **no** evidence of decimalization; current contract specs still describe
  ticks in fractions of a cent.
- The Fair Packaging and Labeling Act result above runs directly against a general anti-fraction
  reading.

Two lines of attack were pursued and **found nothing**, which is worth recording because both are
commonly assumed to support a decimal-only rule. The BIPM SI Brochure was fetched in full and
contains no style rule preferring decimals over fractions for ordinary numerals — every occurrence
of "fraction" is either a dimensionless ratio quantity or historical definition text. ISO 80000-1 is
paywalled and its public preview contains no fraction guidance at all. A NIST prohibition **does**
exist but is narrower than commonly cited, and applies only once a value is in metric units
[Primary]:

> "Binary fractions (such as 1/2 or 3/8) are not used with metric units. For example, a person's
> weight is given as 70.5 kg, not 70-1/2 kg."
> — NIST Special Publication 1038 § 5

One further finding belongs to storage, not notation, but is included because it is an explicit
named prohibition: SEC EDGAR's XBRL validation rejects the `fractionItemType` data type outright
(EFM v68 § 6.7.31, severity Error). That is a schema rule about machine interchange — it says
nothing about how a human writes a number in the filing's prose.

### 3. What authoring tools accept, display, and store — three different answers

This is where notation, display, and storage come apart most sharply, and where the evidence is
most consistent.

| Tool | Accepts typed fraction? | Displays fractions? | Storage | Grade |
|---|---|---|---|---|
| Excel | Only via workaround — bare `1/3` parses as a **date**; needs `0 1/2` or a pre-formatted cell | Yes — dedicated Fraction format, denominators up to 3 digits or fixed halves/…/hundredths | IEEE 754 double | Secondary (vendor) |
| Google Sheets | Same `0 1/2` workaround (community-documented) | Yes — `/` custom format token | Not confirmed | Secondary / Tertiary |
| **DMN / FEEL** (OMG spec) | **No** — the grammar has a `numeric literal` production (digits + optional decimal point) and a *separate* `division` production. `1/3` is a computed expression, not a literal | No facility defined | IEEE 754-2008 **decimal128** ("equivalent to Java BigDecimal with MathContext DECIMAL128") | **Primary (spec)** |
| Drools DRL | No fraction literal in the grammar | No | Depends on host Java type | Secondary |
| Power Fx | No — literal examples are decimal only | No | Decimal (.NET decimal, exact in range) **or** Float (double) | Secondary (vendor) |
| Airtable / Notion / Salesforce / NetSuite / QuickBooks | No | No | Double (Salesforce, explicit); 8-place decimal (NetSuite); others unconfirmed | Secondary |
| HotDocs (document assembly) | **No — vendor states it explicitly** | Yes — but as a *merge-time rendering* of a decimal answer, denominator fixed by an example format, "rounded and simplified" | Decimal | Secondary (vendor) |
| AutoCAD (Architectural / Fractional units) | **Yes** — `12'-3 1/2"` typed directly | Yes — selectable denominator 1…256 | Not confirmed by primary source | Secondary (vendor) |
| Construction Master Pro | **Yes** — numerator, fraction-bar key, denominator | Yes — default 1/16, user-settable 1/2…1/64 | **UNKNOWN** — no vendor statement | Secondary (vendor manual) |

Four things hold across the whole set:

1. **Nothing surveyed stores an exact rational.** Every storage type identified is a binary double, a
   fixed-scale decimal, or IEEE decimal128.
2. **Where fraction notation is accepted, it is an input convenience converted immediately** to the
   tool's native numeric type.
3. **Where fraction display exists, it is a rendering** produced by finding the nearest fraction at a
   bounded denominator. Excel rounds to the nearest representable value; HotDocs' own documentation
   says "Fractions are rounded and simplified."
4. **In every rule/formula grammar actually inspected, `/` is exclusively the division operator,
   never a literal token.** The DMN specification is the clearest evidence because the operator and
   the literal are two distinct numbered grammar productions — a structural distinction, not an
   incidental one.

Point 4 matters for the hypothesis under test: DMN/FEEL is the international standard for
business-analyst-authored decision logic — squarely the audience in question — and it has no
fraction literal, backed by decimal128.

### 4. Fraction-native professional surfaces exist, and the boundary is drawn by the measuring
instrument, not by the audience

Construction and carpentry are a genuine counter-example to "fraction notation is programmer
culture." The Construction Master Pro is a hardware calculator sold to builders, and fractions are
its native input and display mode [Secondary; vendor manual]:

> "Fraction Bar — Used to enter fractions. Fractions may be entered as proper (1/2, 1/8, 1/16) or
> improper (3/2, 9/8). If the denominator (bottom) is not entered, the calculator's fractional
> resolution setting is automatically used"

It defaults to 1/16, exposes a user-settable resolution from 1/2 to 1/64, rounds non-landing results
to the nearest representable fraction, reduces computed fractions to lowest terms, and toggles
between fraction and decimal display rather than showing both. What sits underneath is **not
documented by any vendor surveyed** — recorded as unknown, not guessed.

**But the honest limit is sharp, and it comes from a primary national standard.** NIST Handbook 44
explains the split, and it is not about programmers versus non-programmers [Primary]:

> "if we are concerned only with measurements of length to moderate precision, it is convenient to
> measure and to express these lengths in feet, inches, and binary fractions of an inch... However,
> if these lengths are to be subsequently used to calculate area or volume, that method of
> subdivision at once becomes extremely inconvenient. For that reason, surveyors and civil
> engineers... divide it decimally."

> "machinists, toolmakers, gauge makers, scientists, and others who are engaged in precision
> measurements of relatively small distances... find it convenient to use the inch... but to divide
> the inch decimally... Machinist scales are commonly graduated decimally along one edge and are
> also graduated along another edge to binary fractions as small as 1/64 inch. The scales with
> binary fractions are used only for relatively rough measurements."

So within the same unit system and often the same building: carpentry is fraction-native, precision
machining and surveying are decimal-native, and the driver is the graduation of the measuring tool
and whether area/volume arithmetic follows. Across unit systems the fraction convention largely
disappears — Australian construction drafting uses whole millimetres [Secondary], and even the US
lumber standard states dressed dimensions in **both** millimetres and fractional inches in the same
clause [Primary, PS 20-20].

**Plainly stated: fraction-native notation here is a property of US customary linear measurement at
moderate precision with hand tools, not a general property of non-programmer domain experts.**

### 5. Enterprise ERP: two confirmed exact integer fractions, and a hard boundary around them

The question of whether mainstream enterprise systems represent any quantity as an integer
numerator/denominator pair has a confirmed positive answer in **two** independent vendors — both in
the same context, unit-of-measure conversion — and a clean boundary around it. Seven SAP leads,
plus Oracle, Dynamics, NetSuite, Infor and Workday, were checked; everything outside that one
context was refuted.

**Confirmed — SAP unit-of-measure conversion.** Table `MARM` carries `UMREZ` (numerator) and `UMREN`
(denominator), both 5-digit whole-number packed fields with **zero decimal places**. SAP's own field
documentation makes the intent explicit [Secondary — SAP-authored data-element text recovered via a
third-party mirror; SAP's current help portal is JavaScript-rendered and returned no body text on
direct fetch]:

> "Numerator of the quotient that specifies the ratio of the alternative unit of measure to the base
> unit of measure."
> "Quantity (in alternative unit of measure) = quotient * quantity (in base unit of measure)"
> "Example: ... 5 kg = 3 PC => 1 kg = 3/5 PC"
> "Note: You may enter only whole numbers in the numerator and denominator fields; that is, if
> 3.14 m² correspond to one piece, you must enter integer multiples (314 m² = 100 PC)."

This is a genuine exact fraction: two integers used together as a literal quotient, with the
whole-numbers-only constraint existing precisely so the ratio is exact. The business user types two
separate integer fields in the material master. Whether SAP enforces reduction to lowest terms was
**not confirmed** either way.

**Confirmed — Microsoft Dynamics unit-of-measure conversion, independently.** The AX 2012 core ERP
and Commerce Runtime data model stores two `Int32` fields alongside a decimal one [Primary; Microsoft
Learn]:

> "Numerator and Denominator — With the numerator and the denominator values, you can indicate
> whether the relation between the From unit and the To unit is a 1:1 ratio or if it is a fraction.
> Say, that you want to create a conversion rule for a product where only a half piece fits into a
> box... enter 1 in the Numerator field and 2 in the Denominator field."

> `public int Numerator { get; set; }` / `public int Denominator { get; set; }` /
> `public decimal Factor { get; set; }`

Two vendors, arriving independently at an exact integer ratio pair for the same job, with the
decimal `Factor` kept alongside rather than instead of it. Whether these exact field names persist
in the current non-retail D365 cloud UI was **not** independently confirmed.

**Refuted — SAP currency exchange rates.** The hypothesis that `TCURR`'s `FFACT`/`TFACT` form a
rational numerator/denominator pair does not hold. Primary SAP documentation gives the field types
[Primary; SAP Help Portal legacy static page]:

> "UKURS DEC 9 5 Exchange rate
> FFACT DEC 9 0 Factor for the units of the 'from' currency
> TFACT DEC 9 0 Factor for the units of the 'to' currency"

`UKURS` is a genuine fixed-scale decimal with 5 decimal places; `FFACT`/`TFACT` are integer
**unit-scaling** factors recording that a rate is quoted per 100 or per 1000 units, as SAP's own
guide illustrates: "100 USD equal 92.81993 EUR." That is a decimal rate plus power-of-ten scaling,
not an exact rational.

**Refuted — Oracle, across the board.** UOM conversion (`INV_UOM_CONVERSIONS.CONVERSION_RATE`),
currency (`GL_DAILY_RATES`), BOM component quantities, pricing, and FTE are all single `NUMBER`
decimals. Oracle's documented behavior when a true ratio arises is to evaluate and round it —
fractional FTE is "rounded off to 10 decimal points," discarding exactness rather than retaining a
pair. NetSuite, Infor and Workday likewise showed no integer ratio pair anywhere checked; Workday's
published SOAP schema is explicit, e.g. `Distribution_Percent` is `decimal(9,6)`, documented as
"percentage, represented as a decimal value (e.g., .5)".

**A recurring near-miss worth naming.** Several SAP constructs look like fractions but are
**unnormalized integer weights**: co-product equivalence numbers (`MAKZ-ZIFFR`, `DEC(3,0)`),
settlement equivalence numbers (`COBRB-AQZIF`), and CO allocation "fixed portions." Each row carries
one integer; a receiver's share is its own number divided by the sum of its siblings (30:30:20:20 →
30%/30%/20%/20%). The denominator is **never persisted** — it is recomputed from sibling rows on
every read. Structurally that is a weighted-share list, not a stored p/q pair, and it is a
distinction worth keeping straight.

**The boundary — the most useful finding of this strand.** Ratio-pair representations appear **only
in conversion and scaling contexts**, and **never** in posted financial amounts. SAP's central
posted-amount field `BSEG-DMBTR` is a fixed-scale decimal (11 digits + 2 decimals); Oracle's
`GL_JE_LINES` amounts are plain `NUMBER`; Dynamics F&O amount types are `Real` with 2 decimals by
default; Workday's `Get_Journals` debit and credit amounts are `decimal(26,6)`. Every posted-amount
field examined, in every system examined, is fixed-scale decimal. (NetSuite's posted-amount type and
Infor's column-level types were not reachable — recorded as gaps, not exceptions.)

**And the boundary holds one level deeper.** Even *inside* conversion contexts, most of this math
still collapses to a rounded decimal: Oracle's UOM conversion during pricing (`1/12 → 1.333333` at
configured precision), Oracle and Workday FTE, and SAP's payroll partial-period factor (which is
genuinely computed as `(planned time − absence) ÷ divisor` but is then written into a single field
scaled by a fixed accuracy constant). The two exact-fraction constructs are the **exception even
among conversions**, not the rule.

### 6. What practitioners write when the value genuinely does not terminate

No authority was found anywhere that calls a truncated decimal an *error*. What the regulatory
sources show instead is a consistent structural pattern: **fix a working precision, carry it without
further rounding, round only the final payable amount, and disclose that rounding happened.**

The euro conversion regulation is the most on-point authority located [Primary; mirrored to
`03-exactness-comprehension-excerpts.md`]:

> "1. ... They shall be adopted with six significant figures.
> 2. The conversion rates shall not be rounded or truncated when making conversions. ...
> 4. Monetary amounts to be converted from one national currency unit into another shall first be
> converted into a monetary amount expressed in the euro unit, which amount may be rounded to not
> less than three decimals"
> — Council Regulation (EC) No 1103/97, Article 4

> "Monetary amounts to be paid or accounted for ... shall be rounded up or down to the nearest cent.
> ... If the application of the conversion rate gives a result which is exactly half-way, the sum
> shall be rounded up."
> — same, Article 5

Note what this does **not** do: it does not ask anyone to write a repeating decimal, nor a fraction.
It legislates a finite working precision and the exact moment rounding is permitted.

Tax allocation practice keeps the ratio **symbolic in the formula** and rounds only the dollar output
(the § 752 worked example computes `$100 × $100/$150` and reports $67). IRS Form 8888 dissolves the
problem differently — no fraction or percentage field at all, just whole-dollar amounts constrained
to sum to the total. Accounting standards permit rounded presentation but require disclosure of the
rounding level, and market practice computes percentages from unrounded underlying figures.

Two national legislative drafting authorities were checked directly and are **silent** on the choice.
The US House Legislative Counsel manual's entire fraction guidance is that fractions follow the same
words-versus-figures rules as other numbers; the UK Parliamentary Counsel guidance's entire
percentages subsection reads "Use '%' rather than 'per cent'." Neither treats "write 1/3" versus
"write 33.33%" as a governed question. That is a genuine negative result, checked rather than
assumed.

### 7. The comprehension literature does not transfer — stated plainly

The brief asked for an honest grade, and the honest grade is that this literature cannot carry weight
for the question at hand. Three independent reasons:

- **The most-cited result is about a different axis entirely.** Gigerenzer & Hoffrage (1995),
  *Psychological Review* 102(4), 684–704, is routinely invoked as fraction-versus-decimal evidence.
  Read directly, its independent variable is natural-frequency counts versus single-event
  probability — and the paper treats percentage and decimal notation as *the same* condition. It is
  about communicating **uncertain, probabilistic** events, not about notating an already-resolved
  exact quantity.
- **The strongest fraction study is about children, and its authors say so.** Cauté et al. (2026),
  *J. Exp. Child Psychology* 263, 106373, is large and rigorous (n≈26,000) and finds high
  fraction-magnitude error rates — in grades 6 through 10. The authors write: *"an outstanding
  question is to what extent adults finally manage to get a proper understanding of the magnitude of
  fractions."*
- **The adult literature is thin, small-sample, and mixed in direction.** Binzak & Hubbard (2020),
  *Cognition* 199, 104219, argues across six experiments that adults access fraction magnitude
  rapidly *without calculation* — pointing the opposite way. DeWolf & Vosniadou (2011) is n=28
  undergraduates. None of these samples resembles professional business or legal rule authors, and
  none measures error rates for **writing or reading** a rule.

**No located study measures notation error rates for professional business or legal rule authors.**
The literature establishes neither hypothesis. It should not be cited in either direction.

---

## Implications for Precept

Bridging only — no decision proposed.

1. **The unsupported paragraph should be narrowed, not simply inverted.** The claim that fraction
   notation is a programmer-culture artifact is contradicted by current primary sources: deeds,
   intestacy statutes, corporate voting thresholds, FOMC rate language, a federal packaging
   regulation that names permitted denominators, Treasury quoting, and a hardware calculator sold to
   builders. But the opposing claim — that fraction notation is broadly native to business — is
   equally unsupported. Currency, prices, posted ledger amounts, equity percentages, conversion
   ratios, and every business-rule formula grammar inspected are decimal.

2. **The dividing line found is by quantity kind, and Precept's type system already draws lines in
   roughly that region.** Fractions appear for definitionally-exact shares of a whole; decimals for
   measured, priced, or computed magnitudes. Precept's `money` / `price` / `exchangerate` types sit
   entirely on the decimal side of that line by the evidence. Ownership and allocation shares — the
   fraction-native side — do not currently have a distinguished representation.

3. **DMN/FEEL is the closest comparator for the audience Precept targets**, and it is a decimal128
   language whose grammar deliberately separates the division operator from the numeric literal.
   That is the single most directly transferable data point in this survey for a business-rule DSL.

4. **The confirmed exact-fraction representations in enterprise software are both unit conversion
   factors** (SAP `UMREZ`/`UMREN`; Dynamics AX2012 `Numerator`/`Denominator`), independently arrived
   at by two vendors, and both bounded away from posted amounts. Precept already accepts full UCUM
   including imperial atoms, so unit conversion is the part of Precept's surface nearest to the one
   context where mainstream enterprise software chose exact integer ratios over a decimal factor.
   Notably, both vendors keep a decimal factor **alongside** the pair rather than instead of it.

5. **The regulatory pattern for non-terminating values is "fix a working precision, defer rounding,
   round the payable amount, disclose it."** Precept's `maxplaces` constraint is decimal-places-based
   and is validation rather than auto-rounding, which is a different mechanism from what the euro
   regulation describes. Whether that gap matters is a design question this survey does not answer.

6. **Notation, display, and storage are separable in practice, and every surveyed tool separates
   them.** Excel stores a double and displays a fraction; HotDocs stores a decimal and renders a
   fraction at merge time; AutoCAD accepts fraction input and stores something else. Whatever
   Precept does about notation is not forced by what it does about storage, and vice versa.

## Conclusions

This survey proposes **no decision**. It reports one evidential conclusion about the prior doc's
uncited claim, with the four required legs.

**Conclusion — the "fraction notation is programmer culture" claim is not supported, and neither is
its inverse; the evidence supports a quantity-kind split instead.**

- **Rationale.** Fraction notation appears in current primary legal and regulatory sources written
  by and for non-programmers (deeds, intestacy statutes, Delaware corporate law, FOMC statements,
  16 CFR § 500.17, NIST Handbook 44, trades calculators). Simultaneously, decimal notation
  exclusively governs currency, prices, posted amounts, equity percentages, and every
  business-rule formula grammar inspected. A single-axis claim in either direction contradicts
  primary evidence.
- **Alternatives considered and rejected.** *(i)* "Decimal is universal business notation" —
  rejected; contradicted by the FPLA regulation, the deed corpus, and Treasury quoting.
  *(ii)* "Fractions are broadly native to business authors" — rejected; contradicted by DMN/FEEL's
  grammar, the ERP posted-amount boundary, currency practice, and the SEC decimalization record.
  *(iii)* "The comprehension literature settles it" — rejected; the literature is about children,
  about probabilistic risk, or drawn from small lab samples, and does not transfer.
- **Precedent.** Twelve quantity-kind rows in § 1, each backed by a primary source; NIST Handbook 44
  for the mechanism behind the trades split; the OMG DMN specification for the business-rule-language
  comparator; SAP `MARM` and Dynamics AX2012 `Numerator`/`Denominator` for the two independently
  confirmed enterprise exact fractions, with `BSEG-DMBTR`, `GL_JE_LINES` and Workday `Get_Journals`
  for the posted-amount boundary.
- **Tradeoff accepted.** This conclusion is less actionable than a single-direction answer. It does
  not tell a design pass which notation to prefer; it says the question is under-specified until the
  *kind of quantity* is named. That is a real cost, accepted because the evidence does not support a
  cleaner claim.

## What would change this conclusion

- **If a business-rule language or decision-modeling standard aimed at non-programmers is found with
  a genuine fraction literal in its grammar** (not `/` as a division operator), the § 3 finding that
  rule languages uniformly lack fraction literals would fall, and the DMN comparator would weaken.
- **If a posted financial amount, statutory reporting figure, or G/L line item is found stored or
  mandated as an exact integer ratio pair** in any mainstream enterprise system, the § 5 boundary
  ("ratio pairs only in conversion and scaling contexts") is wrong. Two named gaps are where to look
  first: NetSuite's posted-amount field type and Infor's column-level types, neither of which was
  reachable in this pass.
- **If a regulation or accounting standard is found that mandates an *exact fractional* result and
  forbids rounding to a stated decimal precision**, the § 6 pattern flips for that domain, and
  writing a truncated decimal would become an error there rather than accepted disclosed practice.
- **If a study is found measuring notation error rates among professional business, legal, or
  financial rule authors specifically**, § 7's "does not transfer" verdict is replaced by actual
  evidence, in whichever direction it points.

## Open Questions

- Does the deed convention (words plus parenthetical numeral) appear in *modern* residential
  tenancy-in-common conveyances, or is it concentrated in the litigation-prone mineral-rights
  sub-genre that made verbatim text findable? The sample here is skewed and the investigation says so.
- What arithmetic actually backs the trades calculators and CAD fractional unit modes? No vendor
  documents it. This is the difference between "fraction display over decimal storage" and genuine
  exact rational arithmetic in a shipped non-programmer tool, and it remains unknown.
- Does SAP enforce reduction to lowest terms on manually entered `UMREZ`/`UMREN` pairs, and is the
  "continued fraction development" derivation a real documented SAP algorithm? Both unconfirmed.
- Is there a codified convention for the remainder penny when an estate divides equally among three
  beneficiaries? Searches found only "share and share alike" as an operational division rule, with
  no attributable authority on the arithmetic residue.
- ISO 80000-1's full text was not accessible; whether it contains fraction-notation guidance is
  unresolved in either direction.

## Threats to Validity

- **Search-tool summaries overstated primary sources twice, both caught.** One investigation found
  that a claimed NIST SP 811 fraction prohibition does not exist in SP 811 at all (it is in SP 1038),
  and that a claimed BIPM SI Brochure rule does not exist in the fetched full text. Another found
  that a health-literacy claim attributed to a specific paper was not in that paper's text. Both
  corrected before inclusion. This is direct evidence that unverified search summaries in this topic
  area are unreliable, and a reason to spot-check anything here not carried by a mirrored excerpt.
- **Several claims rest on fetch-tool paraphrase rather than raw verified text**, specifically the
  CME tick size, Del. Code § 203, Cal. Prob. Code § 6401, and three of four bond conversion-ratio
  figures. Flagged individually in the fragments. The PDF-extracted sources (ISDA definitions, UPC,
  the WV and TX opinions, the euro regulation) are the strongest because raw text was extracted and
  grepped directly.
- **SAP's current documentation portal is structurally unreachable.** Its pages are JavaScript-rendered
  and return no body text to a plain fetch. The `UMREZ`/`UMREN` documentation text is SAP-authored but
  was recovered from a third-party mirror, so it is graded Secondary despite being SAP's own words.
  The `TCURR` findings rest on two genuinely primary SAP sources and are stronger.
- **Vendor documentation re-grading.** The authoring-tools investigation graded vendor docs Primary;
  this synthesis re-grades them Secondary per repo convention. Anyone reusing cells from that
  fragment should apply the same correction.
- **Absence of evidence in the tool survey is not exhaustive audit.** "No fraction support" findings
  for Airtable, Notion, QuickBooks, Contract Express, Documate, and tax/actuarial software come from
  a bounded search, not a full vendor-documentation audit.
- **Selection bias in the deed sample** — four instruments, all oil-and-gas royalty interests
  litigated in Texas and West Virginia, which is precisely why their text was reproduced verbatim and
  findable.
- **Metric-country trades evidence is the weakest leg.** The Australian claim rests on a secondary AS
  1100 summary plus an explicitly pro-metrication advocacy blog; the UK claim rests on an exam-prep
  slide deck, and the specific BS 1192 convention could not be verified at all.
- **ERP coverage is uneven.** SAP, Oracle and Dynamics are well covered. Infor publishes
  unauthenticated session documentation and Workday publishes an unauthenticated SOAP/XSD schema —
  both better than expected — but neither exposes everything: Infor's column-level numeric precision
  and both vendors' payroll-FTE representation are could-not-confirm rather than negative findings,
  as is NetSuite's posted-amount type.
- **The most load-bearing user-facing ERP claim rests on Secondary evidence.** That the SAP user
  types two integer fields (rather than a single decimal) is well-supported by SAP's own F1-help
  procedure text and a worked entry example, but no vendor screenshot or current primary UI page was
  obtained, because SAP's current help portal is unreachable by direct fetch. A reviewer wanting to
  rely on the "business users really do type fractions" reading should verify this one directly.
- **Adversarial asymmetry is deliberate and disclosed.** One investigation was instructed to search
  specifically for anti-fraction evidence. It reported findings against its own assignment (FPLA,
  Treasuries, grain futures), which is a mark in favor of its honesty, but the overall corpus was
  assembled by strands with directional briefs and should be read as such.

## Sources

Load-bearing excerpts mirrored to `research/references/business-fraction-notation/`
(`01-primary-source-excerpts.md`, `02-authoring-tool-excerpts.md`,
`03-exactness-comprehension-excerpts.md`, `04-counter-evidence-excerpts.md`,
`05-trades-excerpts.md`, `06-erp-excerpts.md`), with raw HTML snapshots for the SEC and FPLA
sources.

| # | Title | Author / org | Identifier + URL | Grade | Access |
|---|---|---|---|---|---|
| 1 | Uniform Probate Code, Art. II § 2-102 | Uniform Law Commission | uniformlaws.org official text | Primary | 2026-07-20 |
| 2 | Cal. Probate Code § 6401 | California Legislature | leginfo.legislature.ca.gov | Primary (via fetch paraphrase) | 2026-07-20 |
| 3 | *Country Roads Minerals, LLC v. Goodwin* | W. Va. Intermediate Ct. App. | decided 2026-05-01; courtlistener.com PDF | Primary | 2026-07-20 |
| 4 | Texas Supreme Court No. 17-0111 (2018), No. 16-0804 (2018) | Supreme Court of Texas | txcourts.gov PDFs | Primary | 2026-07-20 |
| 5 | Del. Code Ann. tit. 8, § 203 | State of Delaware | delcode.delaware.gov | Primary | 2026-07-20 |
| 6 | 2006 ISDA Definitions §§ 4.16, 5.2(a) | ISDA | bank-hosted PDF (isda.org member-walled) | Primary | 2026-07-20 |
| 7 | US Treasury auctioned-securities API | US Treasury | treasurydirect.gov/TA_WS/securities/auctioned | Primary | 2026-07-20 |
| 8 | Cal. Rev. & Tax. Code § 25129 (UDITPA) | California Legislature | leginfo.legislature.ca.gov | Primary | 2026-07-20 |
| 9 | FOMC Minutes, March 15–16 2022 | Federal Reserve | federalreserve.gov | Primary | 2026-07-20 |
| 10 | MGE Energy Form 8-K; B2Gold 6-K exhibit; IDT Holding LLC operating agreement | SEC EDGAR filers | sec.gov EDGAR | Primary | 2026-07-20 |
| 11 | N.Y. UCC § 3-104 | NY State Senate | nysenate.gov | Primary | 2026-07-20 |
| 12 | Instructions for Form 1040 — Rounding Off to Whole Dollars | IRS | irs.gov | Primary | 2026-07-20 |
| 13 | SEC Release 34-42914; Levitt testimony 2000-06-13; Press Release 2024-137 | SEC | sec.gov | Primary | 2026-07-20 |
| 14 | Reg NMS Rule 612 | SEC | 17 CFR § 242.612 | Primary | 2026-07-20 |
| 15 | EDGAR XBRL Guide § 7.7 (EFM v68 § 6.7.31) | SEC staff | sec.gov specifications PDF | Primary | 2026-07-20 |
| 16 | 16 CFR § 500.17 (Fair Packaging and Labeling Act) | FTC | govinfo.gov | Primary | 2026-07-20 |
| 17 | NIST SP 1038 § 5 — Note on Mixed Units and Fractions | NIST | nvlpubs.nist.gov | Primary | 2026-07-20 |
| 18 | NIST Handbook 44 (2023), App. B § 2.4 | NIST | nist.gov | Primary | 2026-07-20 |
| 19 | SI Brochure, 9th ed. (checked; no fraction rule found) | BIPM | bipm.org PDF | Primary | 2026-07-20 |
| 20 | Voluntary Product Standard PS 20-20, American Softwood Lumber Standard | ALSC / US DOC | published standard | Primary | 2026-07-20 |
| 21 | DMN 1.3 specification — grammar rules 22, 35; decimal128 semantics | OMG | omg.org/spec/DMN | Primary | 2026-07-20 |
| 22 | Council Regulation (EC) No 1103/97, Arts. 4–5, Recitals 11–12 | Council of the EU | CELEX 31997R1103; legislation.gov.uk PDF mirror | Primary | 2026-07-20 |
| 23 | SEC Regulation S-X, 17 CFR § 210.4-01(b) | SEC | law.cornell.edu | Primary | 2026-07-20 |
| 24 | IAS 1 §§ 51(e), 53 | IFRS Foundation | 2021 issued text | Primary | 2026-07-20 |
| 25 | Manual on Drafting Style (2022) § 351(b)(3) | US House Office of the Legislative Counsel | house.gov | Primary | 2026-07-20 |
| 26 | Drafting Guidance (19 March 2024) § 2.3.8 | UK Office of the Parliamentary Counsel | gov.uk | Primary | 2026-07-20 |
| 27 | IRS Form 8888 (Rev. Dec. 2025) | IRS | irs.gov | Primary | 2026-07-20 |
| 28 | Gigerenzer & Hoffrage, "How to improve Bayesian reasoning without instruction" | — | *Psychological Review* 102(4):684–704 (1995) | Primary | 2026-07-20 |
| 29 | Cauté, Potier Watkins, He & Dehaene | — | *J. Exp. Child Psych.* 263:106373; DOI 10.1016/j.jecp.2025.106373 | Primary | 2026-07-20 |
| 30 | Binzak & Hubbard | — | *Cognition* 199:104219; DOI 10.1016/j.cognition.2020.104219 | Primary | 2026-07-20 |
| 31 | Fitzsimmons, Thompson & Sidney | — | *JEP:LMC* 46(11):2049–2074; DOI 10.1037/xlm0000839 | Secondary (not fetched) | 2026-07-20 |
| 32 | SAP Help Portal (legacy static), "Exchange Rates for Currencies in Flat Files" | SAP | help.sap.com/doc/saphelp_nw75/… | Primary | 2026-07-20 |
| 33 | SAP AG, "Direct and Indirect Quotation for Exchange Rates," HELP.CAMENG 4.6C | SAP | SAP-authored PDF | Primary | 2026-07-20 |
| 34 | SAP ABAP data-element documentation, UMREZ / UMREN | SAP (third-party-hosted) | sapdatasheet.org mirror | Secondary | 2026-07-20 |
| 35 | Oracle Fusion Data Dictionary — INV_UOM_CONVERSIONS, GL_DAILY_RATES, GL_JE_LINES; Fusion HCM FTE calculation | Oracle | docs.oracle.com | Primary | 2026-07-20 |
| 35a | Dynamics AX 2012 "Unit conversions form"; Commerce Runtime `UnitOfMeasureConversion` Numerator / Denominator / Factor property reference; F&O decimal-point precision | Microsoft | learn.microsoft.com | Primary | 2026-07-20 |
| 35b | SAP Library — Allocation Segment (tracing factors); Material Quantity Calculation; S/4HANA Group Reporting guide; "Partial Period Remuneration (Factoring)" HELP.PYINT | SAP | help.sap.com legacy static pages + SAP-authored PDFs | Primary | 2026-07-20 |
| 35c | SAP field dictionary — MARM-UMREZ/UMREN, BSEG-DMBTR, KONP-KPEIN/KBETR, MAKZ-ZIFFR, COBRB-AQZIF/PROZS, PA0007-EMPCT | third-party mirrors (sapdatasheet.org, leanx.eu) | — | Tertiary | 2026-07-20 |
| 35d | Workday SOAP Web Services API reference — Assign_Costing_Allocation, Import_Currency_Conversion_Rates, Change_Job, Get_Journals | Workday | community.workday.com productionapi XSD docs | Primary | 2026-07-20 |
| 35e | Infor LN session documentation — Conversion Factors (tcibd0103m000), Currency Rates (tcmcs0108m000), Bill of Material (tibom1110m000) | Infor | docs.infor.com | Primary | 2026-07-20 |
| 35f | NetSuite Applications Suite — "Convert Currencies with SuiteScript" | Oracle NetSuite | docs.oracle.com NetSuite Help Center | Primary | 2026-07-20 |
| 36 | Excel number-format and floating-point documentation | Microsoft | support.microsoft.com | Secondary | 2026-07-20 |
| 37 | Power Fx data types reference | Microsoft | learn.microsoft.com | Secondary | 2026-07-20 |
| 38 | Salesforce number-field precision documentation | Salesforce | developer.salesforce.com | Secondary | 2026-07-20 |
| 39 | HotDocs number-variable / fraction-format documentation | HotDocs | vendor help | Secondary | 2026-07-20 |
| 40 | AutoCAD `-UNITS` command reference | Autodesk | help.autodesk.com | Secondary | 2026-07-20 |
| 41 | Construction Master Pro User's Guide | Calculated Industries | vendor manual PDF | Secondary | 2026-07-20 |
| 42 | "How Does Tick Size Affect Treasury Market Quality?" | Federal Reserve Bank of New York | Liberty Street Economics, Jan 2020 | Secondary | 2026-07-20 |
| 43 | CME Group US Treasury Bond futures contract specifications | CME Group | cmegroup.com | Secondary (search-synthesized) | 2026-07-20 |
| 44 | Exact Rational vs Arbitrary-Precision Decimal (prior internal survey) | Precept research | `research/language/rational-vs-decimal-exact-numeric-representation.md` | Primary (internal) | in-repo |
| 45 | Exact Decimal Arithmetic Survey | Precept research | `research/architecture/compiler/exact-decimal-arithmetic-survey.md` | Primary (internal) | in-repo |
</content>
