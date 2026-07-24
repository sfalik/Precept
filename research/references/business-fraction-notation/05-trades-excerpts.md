# Source excerpts — trades fraction-native authoring surfaces (fragment 05)

Mirror of load-bearing verbatim excerpts for `05-trades-fraction-native.md` (scratchpad findings
fragment feeding the business-fraction-notation research). All fetches this session, 2026-07-20.

---

## 1. Calculated Industries — Construction Master Pro User's Guide

Source: *CONSTRUCTION MASTER® PRO User's Guide* (models Pro/Trig/Desktop), Calculated Industries.
[Primary — vendor's own product manual]
URL: https://www.engineersupply.com/Manuals-and-Help-Docs/Calculated-Industries-Construction-Master-Pro-Manual.pdf
Access date: 2026-07-20. Fetched as PDF, extracted with `pdftotext`.

**Fraction entry mechanics** (p.3 / Key Definitions):

> "Fraction Bar — Used to enter fractions. Fractions may be entered as proper (1/2, 1/8, 1/16) or improper (3/2, 9/8). If the denominator (bottom) is not entered, the calculator's fractional resolution setting is automatically used (e.g., entering 1 5 / = or + will display 15/16, based on the default fractional resolution setting of 16ths."

**Entering dimensions** (p.17, "Entering Linear Dimensions"):

> "When entering Feet-Inch-Fraction values, enter dimensions from largest to smallest — e.g., Feet before Inches, and Inches before Fractions. Enter Fractions by entering the numerator (top), pressing / (fraction bar key), and then the denominator (bottom)."
> "Note: If a denominator is not entered, the fractional setting value is used."

**Default resolution** (p.19-20, "Setting Fractional Resolution"):

> "The Construction Master Pro is set to display fractional answers in 16ths of an Inch. All examples in this User's Guide are based on 1/16". However, you may select the fractional resolution to be displayed in other formats (e.g., 1/64", 1/32", etc.)."

**Selectable denominators — Preference Setting cycle** (p.20):

> "1. Access Preference Settings: Ç ß (Prefs) → FRAC 0-1/16 INCH*
> 2. Access Next Fraction Subsetting: + → FRAC 0-1/32 INCH, FRAC 0-1/64 INCH, FRAC 0-1/2 INCH, FRAC 0-1/4 INCH, FRAC 0-1/8 INCH, FRAC 0-1/16 INCH (repeats)
> 3. To Permanently Set the Fractional Resolution You Have Selected Above, press o (or any key) to set the displayed Fractional Resolution and Exit Preference Settings."
> "*1/16" is the default setting."

Six selectable denominators total: 1/2, 1/4, 1/8, 1/16, 1/32, 1/64.

**Temporary vs. permanent resolution change** (p.21):

> "* Changing the Fractional Resolution on a displayed value does not alter your Permanent Fractional Resolution Setting (set via Preference Settings). Note: This setting is temporary; it will revert back to your permanent fractional setting upon press of o, or when you turn the calculator off."

**Worked rounding example — "Converting a Fractional Value to a Different Resolution"** (p.21):

> "Add 44/64th to 1/64th of an Inch and then convert the answer to other fractional resolutions:
> KEYSTROKE → DISPLAY
> o o → 0.
> 44/64 → 0-44/64 INCH
> +1/64= → 0-45/64 INCH
> Ç 1 (1/16) → 0-11/16 INCH
> Ç 2 (1/2) → 0-1/2 INCH
> Ç 3 (1/32) → 0-23/32 INCH
> Ç 4 (1/4) → 0-3/4 INCH
> Ç 6 (1/64) → 0-45/64 INCH
> Ç 8 (1/8) → 0-3/4 INCH"

Arithmetic check against the raw value (45/64 in. = 0.703125 in.): at 1/16 resolution the nearest
sixteenth is 11/16 (0.6875, distance 0.015625) vs. 12/16 (0.75, distance 0.046875) — correctly
nearest. At 1/32 resolution 0.703125 in. is exactly equidistant between 22/32 and 23/32 (a tie);
the display shows 23/32 (rounds up on exact tie). At 1/8 resolution the nearest eighth is 6/8, and
the calculator displays it reduced as **3/4**, not 6/8 — this is the only reduction visible in this
sequence; the raw typed entry "44/64" is echoed as-is (not reduced to 11/16) when directly typed,
but "6/8" *is* reduced to "3/4" when produced by a resolution conversion. [This distinction — typed
fractions echoed verbatim vs. computed/converted fractions reduced to lowest terms — is an inference
from this one worked example, not a separately stated rule in the manual. Flagged as inferred.]

**Fractional Mode — Standard vs. Constant** (p.83, 85, Appendix B — Preference Settings):

> "12) Fractional Mode
> – *Standard (fractions are displayed to the nearest fraction)
> – Constant (fractions are displayed in the set fractional resolution)"

> "Note: To check the current Fractional Resolution, press ® /. Either "Std" (standard fractional resolution) or "Cnst" (constant) will be displayed, along with the fractional resolution)."

**Factory-default table** (p.82, Appendix B):

> "Fractional Resolution ... 1/16
> ... Fractional Mode ... Standard"

**Decimal ⇄ fraction toggle (sequential, not simultaneous)** (p.3, key definitions for `i`):

> "Inches — Enters or converts to Inches. Also used with the / key for entering fractional Inch values (e.g., 9 i 1 / 2). Note: Repeated presses of i after Ç toggle between Fractional and Decimal Inches (e.g., 9 i 1 / 2 Ç i = 9.5 Inch; press i again to return to Fractional Inches)."

**Worked example, decimal→fraction conversion** (p.23, "Converting Decimal Inches to Fractional Inches"):

> "Convert 9.0625 Inches to Fractional Inches. Then convert to Decimal Feet.
> KEYSTROKE → DISPLAY
> o o → 0.
> 9•0625i → 9.0625 INCH
> Çi → 9-1/16 INCH
> f f* → 0.755208 FEET"

No statement anywhere in this manual describes the internal numeric representation/arithmetic used
by the calculator. **UNKNOWN.**

---

## 2. Calculated Industries — Construction Master III manual (fraction setting page), via ManualsLib

Source: ManualsLib rendering of *Calculated Industries Construction Master III User Manual*, p.20.
[Secondary/mediated — this text was retrieved through the WebFetch tool's HTML-to-summary
conversion of the ManualsLib page, not directly extracted from a raw PDF by this agent, so it
carries slightly lower confidence than the Construction Master Pro PDF above. Cross-check
recommended before treating as fully equivalent to a Primary-grade direct quote.]
URL: https://www.manualslib.com/manual/220274/Calculated-Industries-Construction-Master-Iii.html?page=20
Access date: 2026-07-20.

> "When you turn on your calculator, it is set to display values to the nearest 64th of an inch."
> "the calculator will always show you the lowest common denominator of your fraction — i.e., 1/2 not 16/32."

This is the source for the "reduces to lowest terms" claim on the Construction Master product line
generally; the Construction Master Pro manual (§1 above) does not state this as an explicit rule,
though the worked example there is consistent with it for *computed* fractions.

---

## 3. NIST Handbook 44 (2023) — Appendix B, Units and Systems of Measurement

Source: *NIST Handbook 44-2023, Specifications, Tolerances, and Other Technical Requirements for
Weighing and Measuring Devices*, Appendix B, §2.4 "Subdivision of Units."
[Primary — U.S. national standard handbook, NIST]
DOI: https://doi.org/10.6028/NIST.HB.44-2023
URL fetched: https://www.nist.gov/system/files/documents/2022/11/30/2023%20NIST%20Handbook%2044.pdf
Access date: 2026-07-20. Downloaded and extracted with `pdftotext` (page ~B-9/B-10).

> "2.4. Subdivision of Units. – In general, units are subdivided by one of three methods: (a) decimal, into tenths; (b) duodecimal, into twelfths; or (c) binary, into halves (twos). Usually the subdivision is continued by using the same method. Each method has its advantages for certain purposes, and it cannot properly be said that any one method is "best" unless the use to which the unit and its subdivisions are to be put is known."

> "For example, if we are concerned only with measurements of length to moderate precision, it is convenient to measure and to express these lengths in feet, inches, and binary fractions of an inch, thus 9 feet, 4 3/8 inches. However, if these lengths are to be subsequently used to calculate area or volume, that method of subdivision at once becomes extremely inconvenient. For that reason, surveyors and civil engineers, who are concerned with areas of land, volumes of cuts, fills, excavations, etc., instead of dividing the foot into inches and binary subdivisions of the inch, divide it decimally; that is, into tenths, hundredths, and thousandths of a foot."

> "An illustration of rather complex subdividing is found on the scales used by draftsmen. These scales are of two types: (a) architects, which are commonly graduated with scales in which 3/32, 3/16, 1/8, ¼, 3/8, ½, ¾, 1, 1½, and 3 inches, respectively, represent 1 foot full scale, and also having a scale graduated in the usual manner to 1/16 inch; and (b) engineers, which are commonly subdivided to 10, 20, 30, 40, 50, and 60 parts to the inch."

> "On the other hand, machinists, toolmakers, gauge makers, scientists, and others who are engaged in precision measurements of relatively small distances, even though concerned with measurements of length only, find it convenient to use the inch, instead of the tenth of a foot, but to divide the inch decimally to tenths, hundredths, thousandths, etc., even down to millionths of an inch. Verniers, micrometers, and other precision measuring instruments are usually graduated in this manner. Machinist scales are commonly graduated decimally along one edge and are also graduated along another edge to binary fractions as small as 1/64 inch. The scales with binary fractions are used only for relatively rough measurements."

> "It is seldom convenient or advisable to use binary subdivisions of the inch that are smaller than 1/64. In fact, 1/32-, 1/16-, or 1/8-inch subdivisions are usually preferable for use on a scale to be read with the unaided eye."

Also, elsewhere in HB44 Appendix C on customary-unit tables (secondary confirmation, same document):
binary (fractional) subdivision is explicitly treated as a U.S.-customary-only convention — SI/metric
units are not fractionally subdivided in the handbook's own usage.

---

## 4. Machine Shop VESL (Mt. Hood Community College open textbook) — Lesson 2: Fractional and Decimal Inches

Source: *Machine Shop VESL*, Module 4 Lesson 2, Mt. Hood Community College (Pressbooks open
textbook; the book's own front matter notes it "was archived by its publisher on March 25, 2026").
[Secondary — institutional/community-college vocational textbook, named institutional publisher]
URL: https://mhcc.pressbooks.pub/veslmachinetool/chapter/lesson-2-fractional-and-decimal-inches/
Access date: 2026-07-20. Fetched raw HTML and stripped tags locally (WebFetch tool returned 403).

> "The inch is a basic unit for measuring length within the United States Customary System of measurement. This system is still used everywhere in the trade, although many manufacturers in other countries are using the International Metric System. The Metric System uses decimals to subdivide its units into smaller parts; the U.S. Customary System allows us to use either the fractional inch or the decimal inch."

> "2. The Decimal Inch: It is more common to use the decimal inch in machine shop work than it is to use the fractional inch. An inch may be subdivided into tenths, hundredths, thousands and ten thousandths."

> "1. The Fractional Inch: The fractional inch is an inch divided into smaller pieces which are fractional parts of the inch. Each time the inch is divided to make a smaller unit, the previous unit is divided in half... This continual dividing of the previous unit into two equal pieces next produces eighths; then sixteenths, then thirty-seconds, and finally sixty-fourths. The subdividing stops at sixty-fourths, because that is about as fine a subdivision as we are able to see with our limited eyesight."

---

## 5. FEMA / U.S. Fire Administration — "Using Engineer and Architect Scales"

Source: U.S. Fire Administration (National Fire Academy) training handout, "Using Engineer and
Architect Scales."
[Primary — U.S. federal government training publication]
URL: https://www.usfa.fema.gov/downloads/pdf/nfa/engineer-architect-scales.pdf
Access date: 2026-07-20. Downloaded and extracted with `pdftotext`.

> "1. Engineer, or civil, scales, such as 1˝ = 10´ or 1˝ = 50´, are used for measuring roads, water mains and topographical features. The distance relationships also may be shown as 1:10 or 1:50."
> "2. Architect scales, such as 1/4˝ = 1´0˝ (1/48 size) or 1/8˝ = 1´0˝ (1/96 size), are used for structures and buildings."

> "2. Architect scales use fractions and have the following dimensional relationships: 3/32 = 1 foot, 1/4 = 1 foot, 3/4 = 1 foot, 3/16 = 1 foot, 3/8 = 1 foot, 1 inch = 1 foot, 1/8 = 1 foot, 1/2 = 1 foot, 1 1/2 inches = 1 foot"

> "3. Engineer scales have the following dimensional relationships: 1 inch = 10 feet, 1 inch = 40 feet, 1 inch = 20 feet, 1 inch = 50 feet, 1 inch = 30 feet, 1 inch = 60 feet"
> "When using the engineer scale, you must multiply the value you identify by 10. The small lines between the whole numbers represent individual feet, so a point that falls 2 marks to the right of the whole number 4 is interpreted as 42 feet."

This document never uses the word "decimal," but the engineer-scale structure it describes (uniform
subdivision into tens, read as whole-number multiples) is a base-10 subdivision, contrasted with the
architect scale's explicit inch-fraction-per-foot labeling (3/32, 1/8, 3/16, etc.).

---

## 6. Autodesk AutoCAD — `-UNITS` command reference

Source: Autodesk AutoCAD 2020 Help, `-UNITS` (Command).
[Primary — vendor's own product command reference for its own product]
URL: https://help.autodesk.com/cloudhelp/2020/ENU/AutoCAD-Core/files/GUID-D396FBFE-6171-4A89-9E68-6CB082EBE0E1.htm
Access date: 2026-07-20.

> "4 = Architectural 1'-3 1/2""
> "5 = Fractional 15 1/2""
> "Denominator of smallest fraction to display (Available for architectural or fractional formats)"
> "Specifies the fractional precision: 1, 2, 4, 8, 16, 32, 64, 128, or 256."

No statement in this reference page describes AutoCAD's internal numeric storage type for a
dimension value (i.e., whether the fractional display is computed from a double-precision float,
a fixed-point value, or something else). **UNKNOWN.**

---

## 7. Autodesk Fusion — units/fractional display support articles

Source: Autodesk Fusion support articles (aggregated via WebSearch tool synthesis, not independently
fetched and quoted verbatim page-by-page this session).
[Secondary — vendor support documentation, mediated through WebSearch tool summary rather than a
direct WebFetch quote; treat with slightly lower confidence than §6 above]
URLs:
- https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/How-to-use-fractional-units-in-a-2D-Drawing-created-with-Fusion-360.html
- https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/How-to-change-the-dimensions-from-decimal-format-to-fractional-format.html
Access date: 2026-07-20 (WebSearch synthesis only; not independently re-verified via WebFetch).

Reported (paraphrase, not verbatim — flagged): Fusion's Preferences → Units and Value Display
exposes Decimal / Fractional / Architectural display modes; for 2D Drawings the fractional-units
option requires the source model file to be set to inches.

---

## 8. ALSC / U.S. DOC Voluntary Product Standard PS 20-20 — American Softwood Lumber Standard

Source: *Voluntary Product Standard PS 20-20, American Softwood Lumber Standard*, American Lumber
Standard Committee (ALSC), maintained per U.S. Department of Commerce procedures.
[Primary — national voluntary product standard]
URL: https://www.alsc.org/greenbook%20collection/ps20.pdf
Access date: 2026-07-20. Downloaded and extracted with `pdftotext`.

> "2.16 Nominal size-- The label designation for lumber size categories that does not reflect the dressed size. The nominal size is greater than the dressed size i.e., a dry 2 by 4 is surfaced to 38.1 mm by 88.9 mm (1 1/2 by 3 1/2 inches) [see 3.4.4]."

Note: the standard states the dressed dimension in **both** millimetres and fractional inches side
by side in this defining clause — the fractional-inch form is not the standard's exclusive notation
even for a document governing a U.S. customary trade; it is paired with an SI equivalent.

---

## 9. Australian technical-drawing practice — AS 1100 series (via astcad.com.au)

Source: astcad.com.au (an Australian CAD-training/drafting-services business), summarizing the
AS 1100 series (Standards Australia).
[Secondary — vendor/training-company summary of a national standard, not the standard document
itself; the standard (AS 1100.101, AS 1100.201, AS 1100.301, AS 1100.401, AS 1100.501) was not
directly fetched this session]
URL: https://astcad.com.au/engineering-drawing-tips-to-improve-engineering-drawing-skills/
Access date: 2026-07-20.

> "Millimetres (mm) is the standard unit for Australian mechanical and structural drawings. Do not mix mm and metres on the same drawing."
> "Dimensions below 1mm use 0.x notation (e.g. 0.5, not .5)"

---

## 10. "The Metric Maven" — Building a Metric Shed (metrication advocacy blog)

Source: Randy Bancroft, "Building a Metric Shed," *The Metric Maven* (self-published blog devoted to
advocating U.S. metrication).
[Secondary — named-author article, but explicitly advocacy-oriented; treat the framing as biased even
though the specific factual claims about Australian practice are independently plausible given §9
above. Some claims inside this article are themselves attributed by the author to a third party
(Pat Naughtin, an Australian metrication consultant) and were not independently traced to a primary
Australian government document this session.]
URL: https://themetricmaven.com/building-a-metric-shed/
Access date: 2026-07-20.

> "I explained that in Australia, nothing but millimeters are used for their building construction. The numbers are all simple integers, and no addition or subtraction with decimal points are needed."
> "Use of mm leads to integers for all building dimensions and nearly all building product dimensions, so use of the decimal point is almost completely eliminated."
> "No Feet and inches, no fractions, no decimal points, and just one unit used for all sizes."

Quoted within the same article, attributed by the author to an Australian metric-conversion source
(not independently verified against a primary Australian document this session — flagged Tertiary
within this Secondary source):

> "The metric units for linear measurement in building and construction will be the metre (m) and the millimetre (mm)... the centimetre (cm) shall not be used."

---

## 11. UK technical-drawing convention — Scottish exam-prep slide deck (BSI-derived)

Source: "Graphic Communication BSI Drawing Standards & Dimensioning," a Scottish secondary-school
(National 4/5-level) exam-preparation slide deck hosted on Glow (Scottish schools' national
platform), summarizing British Standards Institution (BSI) drawing conventions.
[Tertiary/weak-Secondary — an educational exam-prep resource, not the BSI standard itself; the
underlying BS document (likely BS 8888) was not independently fetched this session]
URL: https://blogs.glowscotland.org.uk/gc/public/springburnacaddesignandtechnology/uploads/sites/6970/2017/06/BSi-Drawing-Standards.pdf
Access date: 2026-07-20. Downloaded and extracted with `pdftotext`.

> "Measurements should always be shown in millimetres unless otherwise instructed."
> "Figures that require a decimal marker should use a comma, e.g. 22,1."

**Not independently confirmed this session:** a specific claim (surfaced via WebSearch tool synthesis
only, not fetched and quoted from a primary or clearly-identified secondary document) that UK
construction-industry practice under BS 1192 states "whole numbers indicate millimetres, and
decimalized numbers, to three places of decimals, indicate metres." This phrasing appeared in
WebSearch-tool output referencing an Eng-Tips forum thread ("Metric dimensioning convention" —
Tertiary, forum) but the thread itself returned HTTP 403 on direct fetch and could not be verified
verbatim. **Recorded as Threats to Validity, not used as a load-bearing citation.**

---

## Failed fetches (recorded explicitly)

- `https://www.calculated.com/KnowledgeBase/200833117955.pdf` (Construction Master 5 "Fractional
  Resolution" knowledge-base doc) — HTTP 403 Forbidden. Not obtained by any method this session.
- `https://www.alsc.org/greenbook%20collection/ps20.pdf` via WebFetch tool — returned "cannot
  extract... compressed binary data" on first attempt; recovered by downloading directly with
  `curl` and running `pdftotext` locally (see §8 — succeeded on retry).
- `https://www.usfa.fema.gov/downloads/pdf/nfa/engineer-architect-scales.pdf` via WebFetch tool —
  same binary-extraction failure; recovered the same way (see §5 — succeeded on retry).
- `https://www.eng-tips.com/threads/metric-dimensioning-convention.173450/` — HTTP 403 Forbidden.
  Not obtained; the "whole numbers = mm / 3-decimal = m" UK claim traced to this thread via
  WebSearch-tool synthesis only, could not be independently verified. See §11.
- `https://monsterbolts.com/pages/us-screw-sizes` — HTTP 403 Forbidden. Fastener fractional-sizing
  claim in the findings fragment is sourced from WebSearch-tool synthesis of multiple vendor pages
  (mechanicalc.com, princefastener.com, echosupply.com), none individually fetched and quoted
  verbatim — recorded as Tertiary in the findings fragment.
- `https://www.manualslib.com/manual/2367267/Calculated-Industries-Machinist-Calc-Pro.html` (Machinist
  Calc Pro manual) — WebFetch returned only a partial, low-confidence summary ("the manual doesn't
  clearly define what each mode displays"); not usable as a load-bearing quote. The Machinist Calc
  Pro's own display defaults/rounding rule were **not established** this session — see Open Questions
  in the findings fragment.
