# UCUM Tier 1 Unit Curation for Precept Business Domain Types

> **Source:** ucum-org/ucum @ ec61d23 (`ucum-essence.xml`, v2.2, 2024-06-17)  
> **Goal:** ~150 curated atom/expression codes surfaced in autocomplete, documentation, and builder APIs  
> **Constraint:** Time units (`s`, `min`, `h`, `d`) excluded as standalone types — they are NodaTime concerns.  
>  Counting units (`each`, `case`, `pack`, `dozen`) are opaque dimensionless units handled separately.

---

## 1. Changes to the Current 10-Unit Stub

Current stub:
```csharp
Tier1Codes = ["kg", "m", "s", "mol", "K", "L", "N", "J", "Pa", "[degF]"]
```

| Code | Action | Reason |
|------|--------|--------|
| `kg` | ✅ **Keep** | Kilogram is the canonical mass unit; universally needed |
| `m`  | ✅ **Keep** | Meter is the base SI length unit |
| `s`  | ❌ **Remove** | Second is a time unit → NodaTime. No `quantity in 's'` use case |
| `mol`| ❌ **Remove** | Mole is an amount-of-substance unit, primarily scientific. Not a business-domain quantity for manufacturing/retail/logistics; demoted to Tier 2 (add back if pharma/chemistry workflows are primary targets) |
| `K`  | ✅ **Keep** | Kelvin is needed for industrial/scientific temperature (absolute scale, thermodynamics) |
| `L`  | ✅ **Keep** | Liter is the canonical volume unit in most industries |
| `N`  | ✅ **Keep** | Newton is the SI force unit |
| `J`  | ✅ **Keep** | Joule is the SI energy unit |
| `Pa` | ✅ **Keep** | Pascal is the SI pressure unit |
| `[degF]` | ✅ **Keep** | Fahrenheit is required for US business domains |

**Net:** Remove `s` and `mol`. Add the remaining ~148 units below.

---

## 2. Tier 1 Unit Table — Organized by Dimension Category

### Notation Key
- **`[brackets]`** — UCUM non-metric customary atoms; brackets are part of the code string
- **`prefix+atom`** — valid UCUM by prefix rules (e.g., `km` = kilo + `m`)
- **`derived`** — a UCUM expression (e.g., `m/s`, `m2`); valid UCUM, not a single atom
- **`isSpecial`** — non-linear conversion (temperature offset, logarithm); cannot be multiplied/divided algebraically

---

### 2.1 LENGTH — L (16 units)

| # | UCUM Code | Print Symbol | Common Name | Notes |
|---|-----------|-------------|-------------|-------|
| 1 | `m` | m | meter | SI base unit atom |
| 2 | `dm` | dm | decimeter | prefix d + m; 1 dm³ = 1 L |
| 3 | `cm` | cm | centimeter | prefix c + m; clothing, retail, construction |
| 4 | `mm` | mm | millimeter | prefix m + m; manufacturing tolerances |
| 5 | `km` | km | kilometer | prefix k + m; logistics, geography |
| 6 | `um` | μm | micrometer | prefix u + m; precision mfg, pharma |
| 7 | `nm` | nm | nanometer | prefix n + m; semiconductor, thin film |
| 8 | `Ao` | Å | angstrom | non-metric atom; `isMetric="no"`; nanotech, crystallography |
| 9 | `[in_i]` | in | inch (international) | ⚠ brackets required; `[IN_I]` CI variant |
| 10 | `[ft_i]` | ft | foot (international) | ⚠ brackets; most common foot |
| 11 | `[yd_i]` | yd | yard | ⚠ brackets; textiles, US sports fields |
| 12 | `[mi_i]` | mi | statute mile | ⚠ brackets; US road distances |
| 13 | `[nmi_i]` | n.mi | nautical mile | ⚠ brackets; maritime, aviation |
| 14 | `[fth_i]` | fth | fathom | ⚠ brackets; maritime depth |
| 15 | `[ft_us]` | ft | US survey foot | ⚠ brackets; GIS, land survey; ≠ `[ft_i]` |
| 16 | `[mil_i]` | mil | mil (1/1000 inch) | ⚠ brackets; PCB, film coating, wire gauge |

**Rationale:** Covers all major length contexts — metric (SI) for global manufacturing/healthcare/pharma, plus international customary (inch/foot/yard/mile) for US and UK markets. Nautical mile and fathom for maritime/logistics. `[ft_us]` differs from `[ft_i]` by ~2 ppm (critical in GIS/cadastral surveys). Micrometer and nanometer for precision manufacturing and semiconductor. Mil (1/1000 inch) is standard in PCB and film industries. Angstrom used in crystallography and thin-film deposition.

---

### 2.2 MASS — M (19 units)

| # | UCUM Code | Print Symbol | Common Name | Notes |
|---|-----------|-------------|-------------|-------|
| 1 | `g` | g | gram | SI base atom in UCUM (not kg!) |
| 2 | `kg` | kg | kilogram | prefix k + g; most common mass unit |
| 3 | `mg` | mg | milligram | prefix m + g; pharma dosing |
| 4 | `ug` | μg | microgram | prefix u + g; pharma, food additives |
| 5 | `ng` | ng | nanogram | prefix n + g; environmental, pharma |
| 6 | `t` | t | tonne (metric ton) | UCUM atom `t`; = 1000 kg; logistics |
| 7 | `[lb_av]` | lb | pound (avoirdupois) | ⚠ brackets; US/UK general mass |
| 8 | `[oz_av]` | oz | ounce (avoirdupois) | ⚠ brackets; retail, food |
| 9 | `[ston_av]` | ton | short ton (US ton) | ⚠ brackets; = 2000 lb; US logistics |
| 10 | `[lton_av]` | L.ton | long ton (British ton) | ⚠ brackets; = 2240 lb; UK shipping |
| 11 | `[gr]` | gr | grain | ⚠ brackets; pharma, ballistics |
| 12 | `[oz_tr]` | oz t | troy ounce | ⚠ brackets; precious metals, pharma |
| 13 | `[pwt_tr]` | dwt | pennyweight | ⚠ brackets; jewelry, precious metals |
| 14 | `[stone_av]` | st | stone | ⚠ brackets; UK body weight (14 lb) |
| 15 | `[scwt_av]` | cwt | short hundredweight | ⚠ brackets; = 100 lb; US logistics |
| 16 | `[lcwt_av]` | cwt | long hundredweight | ⚠ brackets; = 112 lb; UK logistics |
| 17 | `[car_m]` | ct | metric carat | ⚠ brackets; = 0.2 g; jewelry, gems |
| 18 | `[oz_ap]` | ℥ | apothecary ounce | ⚠ brackets; pharma (≠ avoirdupois oz) |
| 19 | `[lb_ap]` | lb | apothecary pound | ⚠ brackets; pharma (= 12 apoth. oz) |

**Rationale:** UCUM's base mass atom is `g` (gram), not `kg`. Kilogram is `kg` (kilo+gram). The nano/micro/milli prefixed forms cover pharmaceutical dosing precisely. Tonne (`t`) is the ISO metric ton for logistics. The avoirdupois series covers US/UK commercial mass. Troy units are essential for precious metals trading and apothecary for pharmaceutical compounding. Grain is cross-system (used in both pharmacy and agriculture/firearms). Stone for UK healthcare BMI contexts. Hundredweights for bulk agricultural/industrial trade. Carat for jewelry/gemstones.

---

### 2.3 VOLUME — L³ (27 units)

| # | UCUM Code | Print Symbol | Common Name | Notes |
|---|-----------|-------------|-------------|-------|
| **Metric** | | | | |
| 1 | `L` | L | liter | ISO atom; capital `L` preferred; `l` is alias |
| 2 | `dL` | dL | deciliter | prefix d + L; blood chemistry |
| 3 | `cL` | cL | centiliter | prefix c + L; beverage portion sizes |
| 4 | `mL` | mL | milliliter | prefix m + L; pharma, food, lab |
| 5 | `uL` | μL | microliter | prefix u + L; lab, pharma assays |
| 6 | `nL` | nL | nanoliter | prefix n + L; microfluidics |
| 7 | `st` | st | stere | UCUM atom; = 1 m³; timber/firewood |
| **US Fluid** | | | | |
| 8 | `[gal_us]` | gal | US gallon | ⚠ brackets; = 231 in³ (Queen Anne's) |
| 9 | `[qt_us]` | qt | US quart | ⚠ brackets; = ¼ gal |
| 10 | `[pt_us]` | pt | US pint | ⚠ brackets; = ½ qt |
| 11 | `[foz_us]` | fl oz | US fluid ounce | ⚠ brackets; pharma, food |
| 12 | `[cup_us]` | cup | US cup | ⚠ brackets; food/bev recipes |
| 13 | `[tbs_us]` | Tbsp | US tablespoon | ⚠ brackets; food/bev |
| 14 | `[tsp_us]` | tsp | US teaspoon | ⚠ brackets; pharma, food |
| 15 | `[bbl_us]` | bbl | US oil barrel | ⚠ brackets; = 42 US gal; petroleum |
| **US Dry** | | | | |
| 16 | `[bu_us]` | bu | US bushel | ⚠ brackets; agriculture |
| 17 | `[pk_us]` | pk | US peck | ⚠ brackets; = ¼ bu; agriculture |
| 18 | `[dqt_us]` | dqt | US dry quart | ⚠ brackets; = ⅛ pk; agriculture |
| **US Cubic** | | | | |
| 19 | `[cin_i]` | cu in | cubic inch | ⚠ brackets; engine displacement |
| 20 | `[cft_i]` | cu ft | cubic foot | ⚠ brackets; HVAC, construction |
| 21 | `[cyd_i]` | cu yd | cubic yard | ⚠ brackets; concrete, bulk materials |
| 22 | `[cr_i]` | cord | cord | ⚠ brackets; = 128 ft³; firewood/timber |
| **British Imperial** | | | | |
| 23 | `[gal_br]` | gal | imperial gallon | ⚠ brackets; = 4.546 L; UK/Canada |
| 24 | `[qt_br]` | qt | imperial quart | ⚠ brackets; = ¼ imp gal |
| 25 | `[pt_br]` | pt | imperial pint | ⚠ brackets; UK beer/cider |
| 26 | `[foz_br]` | fl oz | imperial fluid ounce | ⚠ brackets; UK pharma |
| 27 | `[bu_br]` | bu | imperial bushel | ⚠ brackets; UK agriculture |

**Rationale:** Volume has the most units because the US/UK commercial split creates parallel systems. Liter with SI prefixes covers healthcare (dL, mL), pharma (mL, uL), and lab (uL, nL). US fluid units are essential for food/beverage and pharmaceutical labeling in North America. The oil barrel (`[bbl_us]`) is critical for energy/petroleum logistics. US dry units (bushel, peck, dry quart) are indispensable for agricultural commodity trading. British imperial volume units are required for UK food, drink, and pharma labeling. Cubic foot and cubic yard for construction/HVAC. Cord for timber and firewood industries.

> **Note on `l` vs `L`:** UCUM defines both `l` (lowercase) and `L` (uppercase) as aliases for liter. Use `L` for new code — it's unambiguous (lowercase `l` can be confused with the digit `1`).

---

### 2.4 AREA — L² (14 units)

| # | UCUM Code | Print Symbol | Common Name | Notes |
|---|-----------|-------------|-------------|-------|
| 1 | `m2` | m² | square meter | derived expression; standard SI area |
| 2 | `cm2` | cm² | square centimeter | derived; electronics, textiles |
| 3 | `mm2` | mm² | square millimeter | derived; wire cross-sections |
| 4 | `km2` | km² | square kilometer | derived; territory, geography |
| 5 | `ar` | a | are | UCUM atom; = 100 m²; rarely used alone |
| 6 | `har` | ha | hectare | prefix h + `ar`; = 10,000 m²; agriculture, real estate |
| 7 | `[acr_us]` | acre | US survey acre | ⚠ brackets; = 4840 yd²; US real estate |
| 8 | `[acr_br]` | acre | British acre | ⚠ brackets; UK real estate |
| 9 | `[sin_i]` | sq in | square inch | ⚠ brackets; manufacturing, packaging |
| 10 | `[sft_i]` | sq ft | square foot | ⚠ brackets; US real estate, construction |
| 11 | `[syd_i]` | sq yd | square yard | ⚠ brackets; textiles, construction |
| 12 | `[smi_us]` | sq mi | square mile | ⚠ brackets; territory, agriculture |
| 13 | `[cml_i]` | circ.mil | circular mil | ⚠ brackets; wire/cable cross-sections in US electrical industry |
| 14 | `[srd_us]` | sq rd | square rod | ⚠ brackets; land survey, US cadastral |

**Rationale:** Area units split along SI (m², cm², km²) and customary (acre, sq ft, sq yd) lines. `ar` and `har` (hectare) are the standard for agriculture and European real estate. US/UK acre difference matters for real estate transactions. Square foot is ubiquitous in US commercial real estate. Circular mil is the industry standard for specifying wire gauge cross-sections in the US electrical/cabling industry.

> **Note on derived area expressions:** `m2`, `cm2`, etc. are UCUM expressions (meter with exponent 2), not atoms. They ARE valid UCUM codes and should be in Tier 1 autocomplete. A conformant parser reads `m2` as m·m (square meter).

---

### 2.5 TEMPERATURE — Θ (4 units)

| # | UCUM Code | Print Symbol | Common Name | Notes |
|---|-----------|-------------|-------------|-------|
| 1 | `K` | K | kelvin | SI base unit; `dim="C"`; **case-sensitive: uppercase K** |
| 2 | `Cel` | °C | degree Celsius | `isSpecial="yes"`; non-linear offset; `Cel(1 K)` |
| 3 | `[degF]` | °F | degree Fahrenheit | ⚠ brackets; `isSpecial="yes"`; non-linear; `degf(5 K/9)` |
| 4 | `[degR]` | °R | degree Rankine | ⚠ brackets; = 5/9 K; engineering thermodynamics |

**Rationale:** Four units cover all business temperature contexts: Kelvin for absolute thermodynamic calculations (energy/chemical engineering), Celsius for global commerce and food safety, Fahrenheit for US industry and consumer products, Rankine for US engineering thermodynamics (steam tables, HVAC). Note that Celsius and Fahrenheit are `isSpecial` (non-linear offset conversions) — they cannot be directly multiplied/divided.

> **⚠ UCUM Case Sensitivity:** `K` (kelvin) is the base unit with `dim="C"` in UCUM. Its case-insensitive variant is `K` as well. Do NOT accept `k` for kelvin — `k` is the kilo prefix.

> **⚠ Non-linear temperatures:** `Cel` and `[degF]` use special conversion functions (`cel()` and `degf()`). Arithmetic on these values requires converting to kelvin first. Your quantity type system should flag these as "offset scale" units.

---

### 2.6 ENERGY — M·L²·T⁻² (and POWER — M·L²·T⁻³) (21 units)

Power (M·L²·T⁻³) is listed here since energy and power are inseparable in energy/utilities industry workflows. Energy = Power × Time.

| # | UCUM Code | Print Symbol | Common Name | Notes |
|---|-----------|-------------|-------------|-------|
| **SI Energy** | | | | |
| 1 | `J` | J | joule | SI atom |
| 2 | `kJ` | kJ | kilojoule | prefix k + J; nutrition |
| 3 | `MJ` | MJ | megajoule | prefix M + J; utilities, gas metering |
| 4 | `GJ` | GJ | gigajoule | prefix G + J; energy commodity trading |
| **Caloric** | | | | |
| 5 | `cal_th` | cal | thermochemical calorie | UCUM atom; = 4.184 J; food science |
| 6 | `cal_IT` | cal\_IT | international table calorie | UCUM atom; = 4.1868 J; steam tables |
| 7 | `cal_[15]` | cal₁₅ | calorie at 15 °C | UCUM atom; = 4.18580 J; food science |
| 8 | `kcal_th` | kcal | kilocalorie (thermochemical) | prefix k + `cal_th`; nutrition |
| 9 | `[Cal]` | Cal | nutrition label calorie | ⚠ brackets; = kcal\_th; food labeling |
| **British Thermal** | | | | |
| 10 | `[Btu_IT]` | Btu | British thermal unit (IT) | ⚠ brackets; = 1.05506 kJ; HVAC |
| 11 | `[Btu_th]` | Btu | British thermal unit (th) | ⚠ brackets; = 1.05435 kJ; combustion |
| **Other Energy** | | | | |
| 12 | `eV` | eV | electronvolt | prefix-capable; semiconductor, battery tech |
| 13 | `Gy` | Gy | gray (radiation absorbed dose) | = J/kg; healthcare, nuclear industry |
| **Electrical Energy** | | | | |
| 14 | `W.h` | Wh | watt-hour | derived; small-scale energy metering |
| 15 | `kW.h` | kWh | kilowatt-hour | derived; standard electricity billing unit |
| 16 | `MW.h` | MWh | megawatt-hour | derived; utility/grid-scale trading |
| **Power** | | | | |
| 17 | `W` | W | watt | SI atom; = J/s |
| 18 | `kW` | kW | kilowatt | prefix k + W; industrial machinery |
| 19 | `MW` | MW | megawatt | prefix M + W; power generation |
| 20 | `GW` | GW | gigawatt | prefix G + W; grid-scale generation |
| 21 | `[HP]` | hp | horsepower | ⚠ brackets; = 550 ft·lbf/s; motors, engines |

**Rationale:** Joule and its SI prefixed forms cover all energy trading contexts (kJ for chemistry/food, MJ for gas billing, GJ for commodity contracts). Caloric units are essential for food/beverage and pharmaceutical nutrition labeling — note the three variants of calorie (`cal_th`, `cal_IT`, `cal_[15]`) all differ slightly; `cal_th` is most common in modern food science. `[Cal]` (capital C) is the nutrition label "Calorie" (= 1 kcal). BTU variants matter for HVAC engineering. Kilowatt-hour is the universal retail electricity unit. Watt through GW covers all power generation and industrial motor ratings. Horsepower remains pervasive in US equipment specifications.

> **⚠ Calorie ambiguity:** `cal_th`, `cal_IT`, `cal_[15]`, `cal_m` are four distinct UCUM atoms representing subtly different calories. The unqualified `cal` atom maps to `cal_th`. `[Cal]` (capital C, brackets) is the food-label kilocalorie = `kcal_th`.

> **⚠ Dot notation for derived units:** `kW.h` uses the UCUM period (`.`) for multiplication. This is case-sensitive and `kW.h` ≠ `KW.H`. The `h` here is the UCUM hour atom (not the hecto prefix), parsed correctly because `kW.h` separates it with `.`.

---

### 2.7 PRESSURE — M·L⁻¹·T⁻² (16 units)

| # | UCUM Code | Print Symbol | Common Name | Notes |
|---|-----------|-------------|-------------|-------|
| 1 | `Pa` | Pa | pascal | SI atom; CODE="PAL" (CI variant) |
| 2 | `hPa` | hPa | hectopascal | prefix h + Pa; meteorology (= mbar) |
| 3 | `kPa` | kPa | kilopascal | prefix k + Pa; tire pressure, medicine |
| 4 | `MPa` | MPa | megapascal | prefix M + Pa; hydraulics, materials |
| 5 | `GPa` | GPa | gigapascal | prefix G + Pa; materials science, geology |
| 6 | `bar` | bar | bar | UCUM atom; = 100,000 Pa; process industry |
| 7 | `mbar` | mbar | millibar | prefix m + bar; meteorology (= hPa) |
| 8 | `atm` | atm | standard atmosphere | UCUM atom; = 101325 Pa; chemistry |
| 9 | `att` | at | technical atmosphere | UCUM atom; = kgf/cm²; engineering |
| 10 | `[psi]` | psi | pound per square inch | ⚠ brackets; US pneumatics, tires |
| 11 | `mm[Hg]` | mmHg | millimeter of mercury | prefix m + `m[Hg]`; clinical medicine |
| 12 | `m[Hg]` | mHg | meter of mercury column | UCUM atom; `m[HG]` CI variant |
| 13 | `cm[H2O]` | cmH₂O | centimeter water column | prefix c + `m[H2O]`; clinical/ventilator |
| 14 | `m[H2O]` | mH₂O | meter of water column | UCUM atom; = 9.80665 kPa; hydrology |
| 15 | `[in_i'Hg]` | inHg | inch of mercury column | ⚠ brackets; ⚠ apostrophe; meteorology |
| 16 | `[in_i'H2O]` | inH₂O | inch of water column | ⚠ brackets; ⚠ apostrophe; HVAC, clinical |

**Rationale:** Pressure units are highly industry-specific. Pascal (SI) and its prefixes cover the full range from micropascals to gigapascals. Bar is the dominant pressure unit in process/chemical engineering. Millibar and hectopascal are numerically identical (1 mbar = 1 hPa) and both used in meteorology. Atmosphere for chemistry reference conditions. Technical atmosphere (`att`) for older European industrial equipment. PSI is ubiquitous in US mechanical and pneumatic systems. Mercury column units (mmHg, inHg) dominate clinical medicine (blood pressure) and meteorology respectively. Water column units dominate HVAC (duct pressure) and clinical ventilator/respiratory equipment.

> **⚠ Embedded brackets in atoms:** `m[Hg]` and `m[H2O]` have square brackets *within* the atom name (they are standard atoms, not customary units that happen to use brackets). Prefixes can be applied: `mm[Hg]` = milli + `m[Hg]`, `cm[H2O]` = centi + `m[H2O]`. Both `m[Hg]` and `m[H2O]` are `isMetric="yes"`.

> **⚠ Apostrophe in codes:** `[in_i'Hg]` and `[in_i'H2O]` contain apostrophes **inside** the brackets. This is standard UCUM syntax (§5: "Within a matching pair of square brackets the full range of characters 33–126 can be used"). In C# string literals these are: `"[in_i'Hg]"` and `"[in_i'H2O]"`.

---

### 2.8 SPEED — L·T⁻¹ (8 units)

> Note: Although these derived expressions include time components (`s`, `h`, `min`), they are **speed/velocity** dimension units, not temporal quantities. The constraint on time units refers to standalone temporal types (`field Duration as quantity in 'h'`), not to time as a component in derived dimensions.

| # | UCUM Code | Print Symbol | Common Name | Notes |
|---|-----------|-------------|-------------|-------|
| 1 | `m/s` | m/s | meter per second | derived; SI standard |
| 2 | `km/h` | km/h | kilometer per hour | derived; road speed, logistics |
| 3 | `cm/s` | cm/s | centimeter per second | derived; flow rates, conveyor belts |
| 4 | `mm/s` | mm/s | millimeter per second | derived; precision motion, extrusion |
| 5 | `[kn_i]` | kn | knot | ⚠ brackets; UCUM atom; = nmi/h; maritime/aviation |
| 6 | `[mi_i]/h` | mph | mile per hour | derived; US road speed, wind speed |
| 7 | `[ft_i]/s` | ft/s | foot per second | derived; US engineering |
| 8 | `[ft_i]/min` | ft/min | foot per minute | derived; HVAC airflow, manufacturing line speed |

**Rationale:** Speed is a derived quantity — most Tier 1 speed "units" are UCUM expressions combining length and time atoms. The only true atomic speed unit is `[kn_i]` (knot), defined as `[nmi_i]/h`. m/s is the SI standard. km/h is universal for transportation. mph dominates US transportation. ft/min is the HVAC standard for air velocity. Knot remains standard in maritime and aviation worldwide.

---

### 2.9 FORCE — M·L·T⁻² (8 units)

| # | UCUM Code | Print Symbol | Common Name | Notes |
|---|-----------|-------------|-------------|-------|
| 1 | `N` | N | newton | SI atom; `isMetric="yes"` |
| 2 | `mN` | mN | millinewton | prefix m + N; precision force sensors |
| 3 | `kN` | kN | kilonewton | prefix k + N; structural engineering |
| 4 | `MN` | MN | meganewton | prefix M + N; heavy civil engineering |
| 5 | `gf` | gf | gram-force | UCUM atom `gf`; `isMetric="yes"` → `kgf` valid |
| 6 | `kgf` | kgf | kilogram-force | prefix k + `gf`; European machinery specs |
| 7 | `[lbf_av]` | lbf | pound-force | ⚠ brackets; = `[lb_av]`·[g]; US engineering |
| 8 | `dyn` | dyn | dyne | CGS atom; = g·cm/s²; legacy scientific |

**Rationale:** Newton and its SI prefixes (mN through MN) cover all modern engineering force contexts. Gram-force (`gf`) is the UCUM atom that enables kilogram-force (`kgf`) via the kilo prefix — `kgf` is extremely common in continental European mechanical engineering specifications. Pound-force (`[lbf_av]`) is ubiquitous in US engineering. Dyne remains in use in some surface science and industrial contexts (dyne/cm = surface tension).

> **Note on `kgf`:** The UCUM atom for gram-force is `gf` (`isMetric="yes"`, class="const"). Applying the kilo prefix gives `kgf` (kilogram-force). This is legitimate UCUM prefix syntax. `kgf` ≈ 9.80665 N.

---

### 2.10 COUNT / DIMENSIONLESS (12 units)

| # | UCUM Code | Print Symbol | Common Name | Notes |
|---|-----------|-------------|-------------|-------|
| 1 | `1` | 1 | unity (dimensionless) | The number 1; ratios, indices |
| 2 | `%` | % | percent | `= 10*-2`; fraction; quality metrics |
| 3 | `[ppm]` | ppm | parts per million | ⚠ brackets; = 10⁻⁶; concentration, purity |
| 4 | `[ppb]` | ppb | parts per billion | ⚠ brackets; = 10⁻⁹; trace contamination |
| 5 | `[ppth]` | ppth | parts per thousand | ⚠ brackets; = 10⁻³; salinity, alcohol |
| 6 | `[pptr]` | pptr | parts per trillion | ⚠ brackets; = 10⁻¹²; trace metals, env |
| 7 | `[pH]` | pH | pH (acidity) | ⚠ brackets; `isSpecial="yes"`; logarithmic; water/food/pharma |
| 8 | `[iU]` | IU | international unit | ⚠ brackets; `[iU]` lowercase i; pharma/nutrition |
| 9 | `[arb'U]` | arb. U | arbitrary unit | ⚠ brackets; ⚠ apostrophe; lab/pharma |
| 10 | `[USP'U]` | U.S.P. | USP unit | ⚠ brackets; ⚠ apostrophe; pharma compendial |
| 11 | `[CFU]` | CFU | colony forming unit | ⚠ brackets; `isArbitrary="yes"`; pharma/food safety |
| 12 | `dB` | dB | decibel | prefix d + `B`(bel); `isSpecial="yes"`; noise monitoring |

**Rationale:** Percent is universal. PPM/PPB/PPth/PPtR cover concentration and trace analysis across food safety, environmental monitoring, and pharmaceutical purity specifications. pH is essential for food/beverage, agriculture, and pharmaceutical manufacturing. International units (IU) are the standard for vitamins and biologics in pharma/nutrition. Arbitrary units cover assay-specific measurements. USP units are required for US pharmacopeia-referenced products. CFU (colony forming unit) is essential in food safety microbiology and pharmaceutical bioburden testing. Decibel covers occupational health noise monitoring in manufacturing.

> **⚠ `[iU]` case sensitivity:** The UCUM code is `[iU]` — lowercase `i`, uppercase `U`. Its case-insensitive variant is `[IU]` (also defined, maps to `[iU]`). Both are in the spec; use `[iU]` as the canonical form.

> **⚠ `[pH]` is `isSpecial`:** pH uses a logarithmic conversion function `pH(1 mol/L)`. It cannot be algebraically combined with other units. Treat similarly to Celsius — special scale.

---

### 2.11 PLANE ANGLE (additional dimension — 5 units)

Although not in the original 10 dimension categories, plane angle is essential for manufacturing (CNC machining, robotics), construction (slope, bearing), navigation, and geospatial applications.

| # | UCUM Code | Print Symbol | Common Name | Notes |
|---|-----------|-------------|-------------|-------|
| 1 | `rad` | rad | radian | SI base unit; `dim="A"` |
| 2 | `deg` | ° | degree of arc | = π/180 rad; most common angle unit |
| 3 | `'` | ′ | arcminute | = deg/60; navigation, surveying |
| 4 | `''` | ″ | arcsecond | = ′/60; GPS, geodesy |
| 5 | `gon` | ᵍ | gon (grade) | = 0.9°; European surveying, topography |

**Rationale:** Degree of arc is universally used in manufacturing CAD/CAM, construction layout, navigation bearings, and equipment alignment. Radian is the SI standard and required for any trigonometric calculation. Arcminute and arcsecond are used in surveying, GPS, and precision astronomy/navigation. Gon (grade) is the European surveying standard where a full circle = 400 gon.

> **⚠ `'` and `''` as UCUM codes:** The arcminute code is a single apostrophe character (`'`) and arcsecond is two apostrophes (`''`). In C# string literals: `"'"` and `"''"`. These are for **plane angle** — NOT time minutes/seconds.

---

## 3. Summary Count

| Dimension Category | Count |
|-------------------|-------|
| Length | 16 |
| Mass | 19 |
| Volume | 27 |
| Area | 14 |
| Temperature | 4 |
| Energy + Power | 21 |
| Pressure | 16 |
| Speed | 8 |
| Force | 8 |
| Count / Dimensionless | 12 |
| Plane Angle (additional) | 5 |
| **TOTAL** | **150** |

---

## 4. UCUM Code Syntax Reference

### 4.1 Case Sensitivity Rules

UCUM has TWO modes:
- **Case-sensitive (CS):** `Code` attribute in the XML — the canonical codes used in Precept
- **Case-insensitive (CI):** `CODE` attribute — uppercase fallback variant

For Precept, **always use case-sensitive codes** (the `Code` attribute). Key pitfalls:

| Code | Correct CS | Wrong | Issue |
|------|-----------|-------|-------|
| Kelvin | `K` | `k` | `k` is kilo prefix |
| Liter | `L` or `l` | `l` (prefer `L`) | `l` vs `1` visual ambiguity |
| Milligram | `mg` | `MG` | `M` = mega prefix → `Mg` = megagram |
| Pascal | `Pa` | `PA` | `PA` not defined in CS mode |
| Celsius | `Cel` | `CEL`, `cel` | Must be exactly `Cel` |
| Nutrition calorie | `[Cal]` | `[CAL]`, `[cal]` | Must be `[Cal]` — capital C |
| International unit | `[iU]` | `[IU]`, `[iu]` | Must be `[iU]` — lowercase i, uppercase U |
| Watt | `W` | `w` | Unrecognized |
| Gram-force | `gf` | `Gf` | Unrecognized |

### 4.2 Bracket Syntax Rules

Square brackets `[…]` are **lexical elements** of the UCUM atom, not delimiters:
- Brackets must be matched pairs
- The full atom code including brackets must be stored and compared as-is
- Prefixes do NOT span bracket boundaries: `k[lb_av]` is **invalid** (kilo-pound does not exist in UCUM)
- The content inside brackets may include apostrophes: `[in_i'Hg]`, `[arb'U]`, `[USP'U]`

### 4.3 Derived Expression Syntax

UCUM expressions for derived units:
- Multiplication: period `.` → `kg.m/s2` (force), `kW.h` (energy)  
- Division: solidus `/` → `m/s`, `mg/dL`
- Exponents: immediately after unit → `m2`, `m3`, `cm2`
- Negative exponents: minus sign → `s-1` (per second = hertz)

Speed examples:
```
m/s        → meter per second
km/h       → kilometer per hour  
[mi_i]/h   → mile per hour
[ft_i]/min → foot per minute
```

Energy/electrical examples:
```
kW.h    → kilowatt-hour (dot = multiplication)
MW.h    → megawatt-hour
J/mol   → joule per mole (molar energy)
```

### 4.4 Special/Non-linear Units

These units use conversion **functions**, not linear factors. They cannot be freely multiplied or divided:

| UCUM Code | Conversion Function | Implication |
|-----------|-------------------|-------------|
| `Cel` | `cel(1 K)` | Celsius = Kelvin − 273.15 |
| `[degF]` | `degf(5 K/9)` | Fahrenheit non-linear offset |
| `[pH]` | `pH(1 mol/L)` | Logarithmic: pH = −log₁₀[H⁺] |
| `dB` | `lg(1 …)` | Logarithmic ratio |

**Recommendation:** Tag these codes as `IsOffsetScale = true` or `IsLogarithmic = true` in Precept's unit metadata to prevent illegal arithmetic operations.

---

## 5. Industry Coverage Rationale

| Industry | Key Tier 1 Units Used |
|----------|----------------------|
| **Manufacturing** | `mm`, `um`, `[mil_i]`, `kg`, `t`, `[lb_av]`, `N`, `kN`, `Pa`, `MPa`, `[psi]`, `Cel`, `[degF]`, `m/s`, `mm/s`, `kW`, `[HP]` |
| **Healthcare** | `kg`, `mg`, `ug`, `[lb_av]`, `[stone_av]`, `mL`, `uL`, `dL`, `K`, `Cel`, `[degF]`, `kPa`, `mm[Hg]`, `cm[H2O]`, `[pH]`, `[iU]`, `[CFU]` |
| **Retail** | `kg`, `g`, `[lb_av]`, `[oz_av]`, `L`, `mL`, `[gal_us]`, `[foz_us]`, `m`, `cm`, `[in_i]`, `[ft_i]`, `Cel`, `[degF]`, `%` |
| **Logistics/Supply Chain** | `kg`, `t`, `[lb_av]`, `[ston_av]`, `[lton_av]`, `[scwt_av]`, `[lcwt_av]`, `L`, `m3` (= `st`), `[bbl_us]`, `km`, `[mi_i]`, `[nmi_i]`, `[kn_i]`, `km/h` |
| **Energy/Utilities** | `kW`, `MW`, `GW`, `kW.h`, `MW.h`, `GJ`, `[Btu_IT]`, `Pa`, `MPa`, `bar`, `[psi]`, `Cel`, `K`, `[degF]` |
| **Construction** | `m`, `cm`, `mm`, `[in_i]`, `[ft_i]`, `[yd_i]`, `m2`, `[sft_i]`, `har`, `[acr_us]`, `m3` (`st`), `[cft_i]`, `[cyd_i]`, `kg`, `t`, `[lb_av]`, `kN`, `MPa`, `[psi]` |
| **Food & Beverage** | `kg`, `g`, `mg`, `[lb_av]`, `[oz_av]`, `L`, `mL`, `[gal_us]`, `[foz_us]`, `[cup_us]`, `[tbs_us]`, `[tsp_us]`, `Cel`, `[degF]`, `[Cal]`, `kcal_th`, `[pH]`, `%` |
| **Pharmaceuticals** | `kg`, `g`, `mg`, `ug`, `ng`, `[gr]`, `[oz_tr]`, `[oz_ap]`, `[lb_ap]`, `mL`, `uL`, `nL`, `[foz_us]`, `Cel`, `K`, `kPa`, `[iU]`, `[USP'U]`, `[arb'U]`, `[CFU]`, `[ppm]`, `[ppb]` |
| **Agriculture** | `kg`, `t`, `[lb_av]`, `[ston_av]`, `[bu_us]`, `[pk_us]`, `[bu_br]`, `L`, `[gal_us]`, `m2`, `har`, `[acr_us]`, `km`, `[mi_i]`, `Cel`, `[degF]`, `[pH]`, `[ppm]` |

---

## 6. Units to Demote to Tier 2

The following are in or near the original stub but should NOT be Tier 1:

| Code | Reason for Tier 2 |
|------|-------------------|
| `s` | Time unit → NodaTime. Remove entirely from `Tier1Codes`. |
| `mol` | Amount-of-substance (scientific). Demote to Tier 2; promote back only if pharma/chemistry domain is primary. |
| `min` | Time unit → NodaTime. |
| `h` | Time unit → NodaTime (note: `h` as **component** of derived units like `km/h` is fine). |
| `d` | Time unit → NodaTime. |
| `Hz` | Frequency (T⁻¹ dimension) — valid for manufacturing/vibration analysis but out of scope for the 10 listed business dimensions. Tier 2. |
| `Bq` | Radioactivity (T⁻¹) — healthcare/nuclear only. Tier 2. |
| `Sv` | Dose equivalent (J/kg) — healthcare/nuclear only. Tier 2. |
| `A` | Ampere (electric current) — electrical engineering; not a Tier 1 business quantity type. Tier 2. |
| `V` | Volt — electrical engineering. Tier 2. |

---

## 7. C# Array Literal — `Tier1Codes`

Ready to paste. Grouped by dimension with comments; the actual array is flat strings:

```csharp
/// <summary>
/// Tier 1 UCUM codes: ~150 curated units surfaced in autocomplete, documentation,
/// and builder APIs. All codes are case-sensitive per UCUM §3.
/// Sourced from ucum-org/ucum @ ec61d23 (v2.2, 2024-06-17).
/// </summary>
public static readonly string[] Tier1Codes =
[
    // ── LENGTH (L) ─────────────────────────────────────────────────────────────
    "m",          // meter (SI base)
    "dm",         // decimeter
    "cm",         // centimeter
    "mm",         // millimeter
    "km",         // kilometer
    "um",         // micrometer (μm)
    "nm",         // nanometer
    "Ao",         // angstrom (Å); non-metric atom
    "[in_i]",     // inch (international)
    "[ft_i]",     // foot (international)
    "[yd_i]",     // yard
    "[mi_i]",     // statute mile
    "[nmi_i]",    // nautical mile
    "[fth_i]",    // fathom
    "[ft_us]",    // US survey foot
    "[mil_i]",    // mil (1/1000 inch)

    // ── MASS (M) ────────────────────────────────────────────────────────────────
    "g",          // gram (UCUM base mass atom)
    "kg",         // kilogram
    "mg",         // milligram
    "ug",         // microgram (μg)
    "ng",         // nanogram
    "t",          // tonne (metric ton)
    "[lb_av]",    // pound (avoirdupois)
    "[oz_av]",    // ounce (avoirdupois)
    "[ston_av]",  // short ton (US ton = 2000 lb)
    "[lton_av]",  // long ton (British = 2240 lb)
    "[gr]",       // grain
    "[oz_tr]",    // troy ounce
    "[pwt_tr]",   // pennyweight (troy)
    "[stone_av]", // stone (14 lb; UK body weight)
    "[scwt_av]",  // short hundredweight (100 lb)
    "[lcwt_av]",  // long hundredweight (112 lb)
    "[car_m]",    // metric carat (0.2 g)
    "[oz_ap]",    // apothecary ounce
    "[lb_ap]",    // apothecary pound

    // ── VOLUME (L³) ─────────────────────────────────────────────────────────────
    "L",          // liter (prefer uppercase L over l)
    "dL",         // deciliter
    "cL",         // centiliter
    "mL",         // milliliter
    "uL",         // microliter (μL)
    "nL",         // nanoliter
    "st",         // stere (= 1 m³; timber)
    "[gal_us]",   // US gallon
    "[qt_us]",    // US quart
    "[pt_us]",    // US pint
    "[foz_us]",   // US fluid ounce
    "[cup_us]",   // US cup
    "[tbs_us]",   // US tablespoon
    "[tsp_us]",   // US teaspoon
    "[bbl_us]",   // US oil barrel (42 gal)
    "[bu_us]",    // US bushel
    "[pk_us]",    // US peck
    "[dqt_us]",   // US dry quart
    "[cin_i]",    // cubic inch
    "[cft_i]",    // cubic foot
    "[cyd_i]",    // cubic yard
    "[cr_i]",     // cord (128 ft³; firewood/timber)
    "[gal_br]",   // imperial gallon
    "[qt_br]",    // imperial quart
    "[pt_br]",    // imperial pint
    "[foz_br]",   // imperial fluid ounce
    "[bu_br]",    // imperial bushel

    // ── AREA (L²) ───────────────────────────────────────────────────────────────
    "m2",         // square meter (derived expression)
    "cm2",        // square centimeter
    "mm2",        // square millimeter
    "km2",        // square kilometer
    "ar",         // are (100 m²)
    "har",        // hectare (100 ar = 10,000 m²)
    "[acr_us]",   // US survey acre
    "[acr_br]",   // British acre
    "[sin_i]",    // square inch
    "[sft_i]",    // square foot
    "[syd_i]",    // square yard
    "[smi_us]",   // square mile (survey)
    "[cml_i]",    // circular mil (wire cross-sections)
    "[srd_us]",   // square rod (US land survey)

    // ── TEMPERATURE (Θ) ─────────────────────────────────────────────────────────
    "K",          // kelvin (SI base; absolute; UPPERCASE K required)
    "Cel",        // degree Celsius (isSpecial — offset scale)
    "[degF]",     // degree Fahrenheit (isSpecial — offset scale)
    "[degR]",     // degree Rankine (5/9 K; engineering thermodynamics)

    // ── ENERGY (M·L²·T⁻²) + POWER (M·L²·T⁻³) ──────────────────────────────────
    "J",          // joule (SI)
    "kJ",         // kilojoule
    "MJ",         // megajoule
    "GJ",         // gigajoule
    "cal_th",     // thermochemical calorie (= 4.184 J)
    "cal_IT",     // international table calorie (= 4.1868 J)
    "cal_[15]",   // calorie at 15 °C (= 4.18580 J)
    "kcal_th",    // kilocalorie (thermochemical)
    "[Cal]",      // nutrition label Calorie (= kcal_th; capital C + brackets)
    "[Btu_IT]",   // British thermal unit, international table
    "[Btu_th]",   // British thermal unit, thermochemical
    "eV",         // electronvolt
    "Gy",         // gray (radiation absorbed dose = J/kg)
    "W.h",        // watt-hour (derived; dot = multiplication)
    "kW.h",       // kilowatt-hour (standard electricity billing)
    "MW.h",       // megawatt-hour (utility/grid trading)
    "W",          // watt (SI power atom)
    "kW",         // kilowatt
    "MW",         // megawatt
    "GW",         // gigawatt
    "[HP]",       // horsepower (= 550 ft·lbf/s)

    // ── PRESSURE (M·L⁻¹·T⁻²) ────────────────────────────────────────────────────
    "Pa",         // pascal (SI; CI variant: PAL)
    "hPa",        // hectopascal (= millibar; meteorology)
    "kPa",        // kilopascal
    "MPa",        // megapascal
    "GPa",        // gigapascal
    "bar",        // bar (= 100,000 Pa)
    "mbar",       // millibar (= hPa; meteorology)
    "atm",        // standard atmosphere (= 101325 Pa)
    "att",        // technical atmosphere (= kgf/cm²)
    "[psi]",      // pound per square inch
    "mm[Hg]",     // millimeter of mercury (mmHg; clinical blood pressure)
    "m[Hg]",      // meter of mercury column (atom; prefix-capable)
    "cm[H2O]",    // centimeter water column (ventilator, HVAC)
    "m[H2O]",     // meter of water column (hydrology; atom; prefix-capable)
    "[in_i'Hg]",  // inch of mercury (meteorology, altimetry)
    "[in_i'H2O]", // inch of water column (HVAC, clinical)

    // ── SPEED (L·T⁻¹) ───────────────────────────────────────────────────────────
    "m/s",        // meter per second
    "km/h",       // kilometer per hour
    "cm/s",       // centimeter per second
    "mm/s",       // millimeter per second
    "[kn_i]",     // knot (= nmi/h; maritime, aviation)
    "[mi_i]/h",   // mile per hour
    "[ft_i]/s",   // foot per second
    "[ft_i]/min", // foot per minute (HVAC airflow, line speed)

    // ── FORCE (M·L·T⁻²) ─────────────────────────────────────────────────────────
    "N",          // newton (SI)
    "mN",         // millinewton
    "kN",         // kilonewton
    "MN",         // meganewton
    "gf",         // gram-force (metric atom; enables kgf via prefix)
    "kgf",        // kilogram-force (kilo + gf)
    "[lbf_av]",   // pound-force
    "dyn",        // dyne (CGS; = g·cm/s²)

    // ── COUNT / DIMENSIONLESS ────────────────────────────────────────────────────
    "1",          // unity (dimensionless number; ratios, indices)
    "%",          // percent (= 10⁻²)
    "[ppm]",      // parts per million (= 10⁻⁶)
    "[ppb]",      // parts per billion (= 10⁻⁹)
    "[ppth]",     // parts per thousand (= 10⁻³)
    "[pptr]",     // parts per trillion (= 10⁻¹²)
    "[pH]",       // pH (isSpecial — logarithmic)
    "[iU]",       // international unit (arbitrary; note: lowercase i, uppercase U)
    "[arb'U]",    // arbitrary unit (apostrophe inside brackets)
    "[USP'U]",    // USP unit (apostrophe inside brackets; pharma)
    "[CFU]",      // colony forming unit (microbiology; food safety; pharma)
    "dB",         // decibel (isSpecial — logarithmic; occupational health)

    // ── PLANE ANGLE (additional dimension) ──────────────────────────────────────
    "rad",        // radian (SI base angle unit)
    "deg",        // degree of arc (= π/180 rad)
    "'",          // arcminute (single apostrophe — plane angle, NOT time)
    "''",         // arcsecond (two apostrophes — plane angle, NOT time)
    "gon",        // gon / grade (= 0.9°; European surveying)
];
```

---

## 8. Appendix: Tier 2 Candidates (Not Tier 1)

Units valid in UCUM and accepted for Precept quantity fields, but NOT surfaced in autocomplete:

| UCUM Code | Name | Why Tier 2 |
|-----------|------|------------|
| `s` | second | Time atom → NodaTime |
| `min` | minute | Time atom → NodaTime |
| `h` | hour | Time atom → NodaTime (valid in derived speed/flow expressions) |
| `d` | day | Time atom → NodaTime |
| `wk` | week | Time atom → NodaTime |
| `mo` | month | Time atom → NodaTime |
| `a` | year | Time atom → NodaTime |
| `mol` | mole | Amount-of-substance; scientific not general business |
| `eq` | equivalents | Chemistry; = mol in UCUM |
| `osm` | osmole | Clinical chemistry |
| `Hz` | hertz | Frequency (T⁻¹); engineering/industrial but not in the 10 business dimensions |
| `kHz`, `MHz` | kilohertz, megahertz | Frequency; Tier 2 |
| `Bq` | becquerel | Radioactivity (T⁻¹); nuclear/healthcare only |
| `Ci` | curie | Radioactivity; CGS |
| `Sv` | sievert | Dose equivalent; nuclear/healthcare |
| `A` | ampere | Electric current; electrical engineering |
| `V` | volt | Electric potential; electrical engineering |
| `Ohm` | ohm | Electric resistance; electrical engineering |
| `F` (farad) | farad | Capacitance; electrical engineering |
| `tex` | tex | Linear density (g/km); textiles |
| `[den]` | denier | Linear density; textiles |
| `P` | poise | Dynamic viscosity (CGS); process engineering |
| `cP` | centipoise | Dynamic viscosity; pharma/food |
| `St` | stokes | Kinematic viscosity (CGS) |
| `[degRe]` | Réaumur | Temperature; historical only |
| `[Btu_39]`, `[Btu_59]`, `[Btu_60]`, `[Btu_m]` | BTU variants | Specialized thermal engineering |
| `Lmb` | Lambert | Luminance (CGS) |
| `lx` | lux | Illuminance; facility management |
| `cd` | candela | Luminous intensity |
| `erg` | erg | Energy (CGS); legacy |
| `[ly]` | light-year | Astronomical length |
| `AU` | astronomical unit | Astronomical length |
| `[smoot]` | smoot | 67 inches; novelty |
| `[hd_i]` | hand | Equine height (4 inches) |
| `[mil_us]` | US survey mil | Surveying; very niche |
| `[in_us]` | US survey inch | Surveying |
| `[mi_us]` | US survey mile | Surveying |
| `[dr_av]` | dram (avoirdupois) | Very small mass; superseded |
| `[sc_ap]` | scruple | Apothecary; very niche |
| `[dr_ap]` | dram (apothecary) | Apothecary |
| `[dpt_us]` | dry pint | Agriculture; minor |
| `[pk_br]` | imperial peck | UK agriculture; minor |
| `[crd_us]` | cord (fluid) | Duplicate of `[cr_i]` |
| `[HPF]` | high power field | Microscopy |
| `[LPF]` | low power field | Microscopy |
| `kat` | katal | Catalytic activity (enzyme kinetics) |
| `U` | unit (enzyme) | Catalytic activity |
| `[IU]` | international unit | CI alias for `[iU]`; use `[iU]` |
| `Gy` | gray | Radiation; consider Tier 1 if healthcare is primary domain |
| `att` | technical atmosphere | Already in Tier 1 |
| `[acr_us]` | US acre | Already in Tier 1 |