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

    public static FrozenDictionary<string, UcumAtom> All => _lazy.Value;

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
