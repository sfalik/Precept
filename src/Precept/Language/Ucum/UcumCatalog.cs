using System.Collections.Frozen;

namespace Precept.Language;

public static class UcumCatalog
{
    private static readonly Lazy<FrozenDictionary<string, UcumAtom>> _lazy = new(LoadTier1);
    private static readonly string[] Tier1Codes =
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

    public static FrozenDictionary<string, UcumAtom> All => _lazy.Value;

    public static UcumParseResult Parse(string expression) => UcumParser.Parse(expression);

    public static bool IsValid(string expression) => Parse(expression).IsValid;

    public static IReadOnlyList<UcumAtom> BrowseTier1() =>
        Tier1Codes.Select(code => All[code]).ToArray();

    public static UcumAtom? LookupAtom(string code) =>
        UcumAtomCatalog.All.TryGetValue(code, out var atom) ? atom : null;

    private static FrozenDictionary<string, UcumAtom> LoadTier1() =>
        Tier1Codes
            .Select(CreateTier1Entry)
            .ToFrozenDictionary(atom => atom.Code, StringComparer.Ordinal);

    private static UcumAtom CreateTier1Entry(string code)
    {
        if (code == "1")
            return new UcumAtom("1", "unity", DimensionVector.None, UcumExactFactor.One, false, null);

        if (UcumAtomCatalog.All.TryGetValue(code, out var atom))
            return atom;

        foreach (var candidate in ExpandParseCandidates(code))
        {
            var result = Parse(candidate);
            if (!result.IsValid || result.Unit is null)
                continue;

            return new UcumAtom(
                code,
                result.Unit.CanonicalCode,
                result.Unit.Vector,
                result.Unit.Scale,
                false,
                null);
        }

        throw new InvalidOperationException($"Tier 1 UCUM code '{code}' could not be parsed.");
    }

    private static IEnumerable<string> ExpandParseCandidates(string code)
    {
        yield return code;

        if (TryInsertExponentMarker(code, out var normalized))
            yield return normalized;
    }

    private static bool TryInsertExponentMarker(string code, out string normalized)
    {
        normalized = string.Empty;
        var suffixStart = code.Length;
        while (suffixStart > 0 && char.IsDigit(code[suffixStart - 1]))
            suffixStart--;

        if (suffixStart == 0 || suffixStart == code.Length)
            return false;

        normalized = $"{code[..suffixStart]}^{code[suffixStart..]}";
        return true;
    }
}
