using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Precept.Language;

public static class UcumAtomCatalog
{
    private const string ResourceName = "Precept.Data.Ucum.ucum-essence.xml";

    private static readonly Lazy<FrozenDictionary<string, UcumAtom>> _lazy = new(Load);
    private static readonly Lazy<IReadOnlyList<UcumAtom>> _lazyTier1 = new(LoadTier1);
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

    public static IReadOnlyList<UcumAtom> BrowseTier1() => _lazyTier1.Value;

    private static FrozenDictionary<string, UcumAtom> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");

        var document = XDocument.Load(stream);
        var resolved = new Dictionary<string, UcumAtom>(StringComparer.Ordinal);
        SeedIntrinsicAtoms(resolved);

        var pending = document.Root?
            .Elements()
            .Where(IsAtomElement)
            .Select(CreatePendingAtom)
            .Where(atom => atom is not null && !resolved.ContainsKey(atom.Code))
            .Cast<PendingAtom>()
            .ToList() ?? [];

        while (pending.Count > 0)
        {
            var progress = false;

            for (var index = pending.Count - 1; index >= 0; index--)
            {
                var candidate = pending[index];
                if (!TryEvaluate(candidate.Expression, resolved, out var evaluation))
                    continue;

                resolved[candidate.Code] = new UcumAtom(
                    candidate.Code,
                    candidate.Name,
                    evaluation.Vector,
                    evaluation.Scale,
                    candidate.Prefixable,
                    null);
                pending.RemoveAt(index);
                progress = true;
            }

            if (!progress)
                throw new InvalidOperationException(
                    $"Unable to resolve UCUM unit definitions: {string.Join(", ", pending.Select(atom => atom.Code))}");
        }

        return resolved.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static IReadOnlyList<UcumAtom> LoadTier1() =>
        Tier1Codes.Select(CreateTier1Entry).ToArray();

    private static UcumAtom CreateTier1Entry(string code)
    {
        if (code == "1")
            return new UcumAtom("1", "unity", DimensionVector.None, UcumExactFactor.One, false, null);

        if (All.TryGetValue(code, out var atom))
            return atom;

        foreach (var candidate in ExpandParseCandidates(code))
        {
            var result = UcumParser.Parse(candidate);
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

        throw new InvalidOperationException($"Tier 1 UCUM code '{code}' was not found in UcumAtomCatalog.All and could not be derived via parsing.");
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

    private static void SeedIntrinsicAtoms(IDictionary<string, UcumAtom> atoms)
    {
        atoms["m"] = new UcumAtom("m", "meter", new DimensionVector(1, 0, 0, 0, 0, 0, 0), UcumExactFactor.One, true, null);
        atoms["s"] = new UcumAtom("s", "second", new DimensionVector(0, 0, 1, 0, 0, 0, 0), UcumExactFactor.One, true, null);
        atoms["kg"] = new UcumAtom("kg", "kilogram", new DimensionVector(0, 1, 0, 0, 0, 0, 0), UcumExactFactor.One, false, null);
        atoms["A"] = new UcumAtom("A", "ampere", new DimensionVector(0, 0, 0, 1, 0, 0, 0), UcumExactFactor.One, true, null);
        atoms["K"] = new UcumAtom("K", "kelvin", new DimensionVector(0, 0, 0, 0, 1, 0, 0), UcumExactFactor.One, true, null);
        atoms["mol"] = new UcumAtom("mol", "mole", new DimensionVector(0, 0, 0, 0, 0, 1, 0), UcumExactFactor.One, true, null);
        atoms["cd"] = new UcumAtom("cd", "candela", new DimensionVector(0, 0, 0, 0, 0, 0, 1), UcumExactFactor.One, true, null);
        atoms["g"] = new UcumAtom("g", "gram", new DimensionVector(0, 1, 0, 0, 0, 0, 0), UcumExactFactor.Parse("1e-3"), true, null);
        atoms["C"] = new UcumAtom("C", "coulomb", new DimensionVector(0, 0, 1, 1, 0, 0, 0), UcumExactFactor.One, false, null);
        atoms["rad"] = new UcumAtom("rad", "radian", DimensionVector.None, UcumExactFactor.One, false, null);
        atoms["each"] = new UcumAtom("each", "each", DimensionVector.None, UcumExactFactor.One, false, null);
    }

    private static bool IsAtomElement(XElement element)
    {
        var localName = element.Name.LocalName;
        return localName is "base-unit" or "unit";
    }

    private static PendingAtom? CreatePendingAtom(XElement element)
    {
        var code = element.Attribute("Code")?.Value;
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var name = element.Elements().FirstOrDefault(child => child.Name.LocalName == "name")?.Value.Trim() ?? code;
        var prefixable = string.Equals(element.Attribute("isMetric")?.Value, "yes", StringComparison.OrdinalIgnoreCase)
                         && code != "kg";

        var expression = GetDefinitionExpression(element, code);
        return expression is null ? null : new PendingAtom(code, name, expression, prefixable);
    }

    private static string? GetDefinitionExpression(XElement element, string code)
    {
        if (code is "m" or "s" or "kg" or "A" or "K" or "mol" or "cd" or "g" or "C" or "rad")
            return null;

        var valueElement = element.Elements().FirstOrDefault(child => child.Name.LocalName == "value");
        if (valueElement is null)
            return null;

        var functionElement = valueElement.Elements().FirstOrDefault(child => child.Name.LocalName == "function");
        if (functionElement is not null)
        {
            var factorText = functionElement.Attribute("value")?.Value ?? "1";
            var unitText = functionElement.Attribute("Unit")?.Value
                           ?? valueElement.Attribute("Unit")?.Value
                           ?? "1";
            return CombineFactorAndUnit(factorText, unitText);
        }

        var valueText = valueElement.Attribute("value")?.Value ?? "1";
        var unitExpression = valueElement.Attribute("Unit")?.Value ?? "1";
        return CombineFactorAndUnit(valueText, unitExpression);
    }

    private static string CombineFactorAndUnit(string factorText, string unitText)
    {
        var factor = factorText.Trim();
        var unit = unitText.Trim();

        if (unit == "1")
            return factor;

        if (factor == "1")
            return unit;

        return $"{factor} {unit}";
    }

    private static bool TryEvaluate(
        string expression,
        IReadOnlyDictionary<string, UcumAtom> atoms,
        out UnitEvaluation evaluation)
    {
        try
        {
            var normalized = StripFunctionWrapper(expression);
            if (normalized.StartsWith("/", StringComparison.Ordinal))
                normalized = $"1{normalized}";

            var parser = new MiniExpressionEvaluator(normalized, atoms);
            evaluation = parser.Parse();
            return true;
        }
        catch
        {
            evaluation = default;
            return false;
        }
    }

    private static string StripFunctionWrapper(string expression)
    {
        var current = expression.Trim();

        while (TryExtractWrappedExpression(current, out var inner))
            current = inner.Trim();

        return current;
    }

    private static bool TryExtractWrappedExpression(string text, out string inner)
    {
        inner = string.Empty;
        var openParen = text.IndexOf('(');
        if (openParen <= 0 || text[^1] != ')')
            return false;

        var depth = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '(')
                depth++;
            else if (text[index] == ')')
                depth--;

            if (depth == 0 && index < text.Length - 1)
                return false;
        }

        if (depth != 0)
            return false;

        inner = text[(openParen + 1)..^1];
        return true;
    }

    private readonly record struct UnitEvaluation(DimensionVector Vector, UcumExactFactor Scale)
    {
        public UnitEvaluation Multiply(UnitEvaluation other) =>
            new(Vector.Multiply(other.Vector), Scale.Multiply(other.Scale));

        public UnitEvaluation Divide(UnitEvaluation other) =>
            new(Vector.Divide(other.Vector), Scale.Divide(other.Scale));

        public UnitEvaluation Pow(int exponent) =>
            new(Vector.Pow(exponent), Scale.Pow(exponent));
    }

    private sealed record PendingAtom(string Code, string Name, string Expression, bool Prefixable);

    private sealed partial class MiniExpressionEvaluator
    {
        private static readonly Regex NumberPattern =
            new(@"^[+-]?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?", RegexOptions.Compiled);

        private readonly string _text;
        private readonly IReadOnlyDictionary<string, UcumAtom> _atoms;
        private int _position;

        public MiniExpressionEvaluator(string text, IReadOnlyDictionary<string, UcumAtom> atoms)
        {
            _text = text;
            _atoms = atoms;
        }

        public UnitEvaluation Parse()
        {
            var evaluation = ParseExpression();
            SkipWhitespace();
            if (_position != _text.Length)
                throw new FormatException($"Unexpected token at position {_position} in '{_text}'.");

            return evaluation;
        }

        private UnitEvaluation ParseExpression()
        {
            var evaluation = ParseProduct();
            while (true)
            {
                SkipWhitespace();
                if (!TryConsume('/'))
                    return evaluation;

                evaluation = evaluation.Divide(ParseProduct());
            }
        }

        private UnitEvaluation ParseProduct()
        {
            var evaluation = ParseComponent();
            while (true)
            {
                SkipWhitespace();
                if (TryConsume('.'))
                {
                    evaluation = evaluation.Multiply(ParseComponent());
                    continue;
                }

                if (!CanStartComponent())
                    return evaluation;

                evaluation = evaluation.Multiply(ParseComponent());
            }
        }

        private UnitEvaluation ParseComponent()
        {
            SkipWhitespace();

            if (TryConsume('('))
            {
                var inner = ParseExpression();
                Expect(')');
                return inner.Pow(ParseOptionalExponent());
            }

            if (TryParseUnitReference(out var unit))
                return unit;

            if (TryParseNumber(out var number))
                return new UnitEvaluation(DimensionVector.None, number);

            throw new FormatException($"Unsupported UCUM expression '{_text}'.");
        }

        private bool TryParseUnitReference(out UnitEvaluation evaluation)
        {
            foreach (var atom in _atoms.Values.OrderByDescending(atom => atom.Code.Length))
            {
                if (!MatchesUnit(atom.Code, allowPrefix: false, out evaluation))
                    continue;

                return true;
            }

            foreach (var prefix in UcumPrefixCatalog.All.Values.OrderByDescending(prefix => prefix.Code.Length).ThenBy(prefix => prefix.Order))
            {
                if (!MatchesUnit(prefix.Code, allowPrefix: true, out evaluation))
                    continue;

                return true;
            }

            evaluation = default;
            return false;
        }

        private bool MatchesUnit(string code, bool allowPrefix, out UnitEvaluation evaluation)
        {
            if (!RemainingText().StartsWith(code, StringComparison.Ordinal))
            {
                evaluation = default;
                return false;
            }

            var start = _position;

            if (!allowPrefix)
            {
                _position += code.Length;
                var exponent = ParseOptionalExponent();
                if (!IsFactorBoundary(_position))
                {
                    _position = start;
                    evaluation = default;
                    return false;
                }

                evaluation = new UnitEvaluation(_atoms[code].Vector, _atoms[code].Scale).Pow(exponent);
                return true;
            }

            var prefix = UcumPrefixCatalog.All[code];
            var remainder = RemainingText()[code.Length..];
            foreach (var atom in _atoms.Values.Where(atom => atom.Prefixable).OrderByDescending(atom => atom.Code.Length))
            {
                if (!remainder.StartsWith(atom.Code, StringComparison.Ordinal))
                    continue;

                _position = start + code.Length + atom.Code.Length;
                var exponent = ParseOptionalExponent();
                if (!IsFactorBoundary(_position))
                {
                    _position = start;
                    continue;
                }

                evaluation = new UnitEvaluation(atom.Vector, prefix.Factor.Multiply(atom.Scale)).Pow(exponent);
                return true;
            }

            _position = start;
            evaluation = default;
            return false;
        }

        private bool TryParseNumber(out UcumExactFactor factor)
        {
            var match = NumberPattern.Match(RemainingText().ToString());
            if (!match.Success)
            {
                factor = default;
                return false;
            }

            var end = _position + match.Length;
            if (!IsFactorBoundary(end))
            {
                factor = default;
                return false;
            }

            _position = end;
            factor = UcumExactFactor.Parse(match.Value);
            return true;
        }

        private int ParseOptionalExponent()
        {
            SkipWhitespace();
            var start = _position;
            if (_position < _text.Length && (_text[_position] == '+' || _text[_position] == '-'))
                _position++;

            var digitsStart = _position;
            while (_position < _text.Length && char.IsDigit(_text[_position]))
                _position++;

            if (digitsStart == _position)
            {
                _position = start;
                return 1;
            }

            return int.Parse(_text[start.._position], CultureInfo.InvariantCulture);
        }

        private bool CanStartComponent()
        {
            SkipWhitespace();
            return _position < _text.Length && _text[_position] is not '.' and not '/' and not ')';
        }

        private bool IsFactorBoundary(int index)
        {
            if (index >= _text.Length)
                return true;

            var current = _text[index];
            if (current == '+' || current == '-')
                return index + 1 < _text.Length && char.IsDigit(_text[index + 1]);

            return char.IsWhiteSpace(current) || current is '.' or '/' or ')';
        }

        private ReadOnlySpan<char> RemainingText() => _text.AsSpan(_position);

        private void SkipWhitespace()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
                _position++;
        }

        private bool TryConsume(char token)
        {
            SkipWhitespace();
            if (_position >= _text.Length || _text[_position] != token)
                return false;

            _position++;
            return true;
        }

        private void Expect(char token)
        {
            if (!TryConsume(token))
                throw new FormatException($"Expected '{token}' in '{_text}'.");
        }
    }
}
