namespace Precept.Language;

public static class UcumParser
{
    public static UcumParseResult Parse(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return UcumParseResult.Failure(new UcumDiagnostic("UCUM001", "UCUM expression cannot be empty.", 0, 0, null));

        var tokens = UcumLexer.Tokenize(expression);
        var lexicalErrors = tokens.Where(token => token.Kind == UcumTokenKind.Error)
            .Select(token => new UcumDiagnostic("UCUM002", "Invalid UCUM token.", token.Start, token.Length, null))
            .ToArray();
        if (lexicalErrors.Length > 0)
            return new UcumParseResult(false, null, lexicalErrors);

        var reader = new TokenReader(tokens);
        var diagnostics = new List<UcumDiagnostic>();
        var expressionNode = ParseExpression(reader, diagnostics);
        if (expressionNode is null)
            return new UcumParseResult(false, null, diagnostics);

        if (reader.Current.Kind != UcumTokenKind.EndOfInput)
        {
            diagnostics.Add(new UcumDiagnostic("UCUM003", "Unexpected token in UCUM expression.", reader.Current.Start, reader.Current.Length, null));
            return new UcumParseResult(false, null, diagnostics);
        }

        var reduced = Reduce(expressionNode);
        DimensionCatalog.TryGetAlias(reduced.Vector, out var alias);

        var unit = new UcumParsedUnit(
            expression,
            reduced.CanonicalCode,
            reduced.Vector,
            reduced.Scale,
            alias?.Name,
            reduced.UsedAtoms.Where(atom => atom.Code != "1").DistinctBy(atom => atom.Code).ToArray(),
            reduced.Annotations.ToArray());

        return UcumParseResult.Success(unit);
    }

    private static UcumExpression? ParseExpression(TokenReader reader, List<UcumDiagnostic> diagnostics)
    {
        var left = ParseProduct(reader, diagnostics);
        if (left is null)
            return null;

        while (reader.Current.Kind == UcumTokenKind.Slash)
        {
            reader.Advance();
            var right = ParseProduct(reader, diagnostics);
            if (right is null)
                return null;
            left = new UcumQuotientNode(left, right);
        }

        return left;
    }

    private static UcumExpression? ParseProduct(TokenReader reader, List<UcumDiagnostic> diagnostics)
    {
        var left = ParseFactor(reader, diagnostics);
        if (left is null)
            return null;

        while (reader.Current.Kind == UcumTokenKind.Dot)
        {
            reader.Advance();
            var right = ParseFactor(reader, diagnostics);
            if (right is null)
                return null;
            left = new UcumProductNode(left, right);
        }

        return left;
    }

    private static UcumExpression? ParseFactor(TokenReader reader, List<UcumDiagnostic> diagnostics)
    {
        var primary = ParsePrimary(reader, diagnostics);
        if (primary is null)
            return null;

        if (reader.Current.Kind == UcumTokenKind.Exponent)
        {
            if (!int.TryParse(reader.Current.Text, out var exponent))
            {
                diagnostics.Add(new UcumDiagnostic("UCUM004", "UCUM exponent must be an integer.", reader.Current.Start, reader.Current.Length, null));
                return null;
            }

            primary = new UcumExponentNode(primary, exponent);
            reader.Advance();
        }

        while (reader.Current.Kind == UcumTokenKind.Annotation)
        {
            primary = new UcumAnnotatedNode(primary, reader.Current.Text);
            reader.Advance();
        }

        return primary;
    }

    private static UcumExpression? ParsePrimary(TokenReader reader, List<UcumDiagnostic> diagnostics)
    {
        if (reader.Current.Kind == UcumTokenKind.Atom)
        {
            var token = reader.Current;
            reader.Advance();
            if (TryResolveAtom(token.Text, out var expression))
                return expression;

            diagnostics.Add(new UcumDiagnostic("UCUM005", $"Unrecognized UCUM atom '{token.Text}'.", token.Start, token.Length, null));
            return null;
        }

        if (reader.Current.Kind == UcumTokenKind.OpenParen)
        {
            var start = reader.Current;
            reader.Advance();
            var inner = ParseExpression(reader, diagnostics);
            if (inner is null)
                return null;

            if (reader.Current.Kind != UcumTokenKind.CloseParen)
            {
                diagnostics.Add(new UcumDiagnostic("UCUM006", "Unbalanced parentheses in UCUM expression.", start.Start, start.Length, null));
                return null;
            }

            reader.Advance();
            return new UcumGroupNode(inner);
        }

        if (reader.Current.Kind == UcumTokenKind.Annotation)
        {
            var annotation = reader.Current.Text;
            reader.Advance();
            return new UcumAnnotatedNode(
                new UcumAtomNode(new UcumAtom("1", "unity", DimensionVector.None, UcumExactFactor.One, false, null)),
                annotation);
        }

        diagnostics.Add(new UcumDiagnostic("UCUM007", "Expected a UCUM atom or grouped expression.", reader.Current.Start, reader.Current.Length, null));
        return null;
    }

    private static bool TryResolveAtom(string text, out UcumExpression expression)
    {
        if (UcumAtomCatalog.All.TryGetValue(text, out var atom))
        {
            expression = new UcumAtomNode(atom);
            return true;
        }

        var prefix = UcumPrefixCatalog.LongestPrefixMatch(text);
        if (prefix is not null)
        {
            var remainder = text[prefix.Code.Length..];
            if (UcumAtomCatalog.All.TryGetValue(remainder, out var prefixedAtom) && prefixedAtom.Prefixable)
            {
                expression = new UcumPrefixedAtomNode(prefix, prefixedAtom);
                return true;
            }
        }

        expression = null!;
        return false;
    }

    private static ReducedUnit Reduce(UcumExpression expression) => expression switch
    {
        UcumAtomNode atom => new(atom.Atom.Vector, atom.Atom.Scale, atom.Atom.Code, [atom.Atom], []),
        UcumPrefixedAtomNode prefixed => new(
            prefixed.Atom.Vector,
            prefixed.Prefix.Factor.Multiply(prefixed.Atom.Scale),
            $"{prefixed.Prefix.Code}{prefixed.Atom.Code}",
            [prefixed.Atom],
            []),
        UcumProductNode product => CombineProduct(Reduce(product.Left), Reduce(product.Right)),
        UcumQuotientNode quotient => CombineQuotient(Reduce(quotient.Left), Reduce(quotient.Right)),
        UcumExponentNode exponent => ApplyExponent(Reduce(exponent.Inner), exponent.Exponent),
        UcumGroupNode group => WrapGroup(Reduce(group.Inner)),
        UcumAnnotatedNode annotated => AddAnnotation(Reduce(annotated.Inner), annotated.Annotation),
        _ => throw new InvalidOperationException($"Unsupported UCUM expression node '{expression.GetType().Name}'."),
    };

    private static ReducedUnit CombineProduct(ReducedUnit left, ReducedUnit right) => new(
        left.Vector.Multiply(right.Vector),
        left.Scale.Multiply(right.Scale),
        $"{left.CanonicalCode}.{right.CanonicalCode}",
        [.. left.UsedAtoms, .. right.UsedAtoms],
        [.. left.Annotations, .. right.Annotations]);

    private static ReducedUnit CombineQuotient(ReducedUnit left, ReducedUnit right)
    {
        var denominatorCode = right.CanonicalCode.Contains('.', StringComparison.Ordinal)
            ? $"({right.CanonicalCode})"
            : right.CanonicalCode;

        return new ReducedUnit(
            left.Vector.Divide(right.Vector),
            left.Scale.Divide(right.Scale),
            $"{left.CanonicalCode}/{denominatorCode}",
            [.. left.UsedAtoms, .. right.UsedAtoms],
            [.. left.Annotations, .. right.Annotations]);
    }

    private static ReducedUnit ApplyExponent(ReducedUnit inner, int exponent) => new(
        inner.Vector.Pow(exponent),
        inner.Scale.Pow(exponent),
        $"{inner.CanonicalCode}^{exponent}",
        inner.UsedAtoms,
        inner.Annotations);

    private static ReducedUnit WrapGroup(ReducedUnit inner) => new(
        inner.Vector,
        inner.Scale,
        $"({inner.CanonicalCode})",
        inner.UsedAtoms,
        inner.Annotations);

    private static ReducedUnit AddAnnotation(ReducedUnit inner, string annotation) => new(
        inner.Vector,
        inner.Scale,
        inner.CanonicalCode,
        inner.UsedAtoms,
        [.. inner.Annotations, annotation]);

    private sealed record ReducedUnit(
        DimensionVector Vector,
        UcumExactFactor Scale,
        string CanonicalCode,
        IReadOnlyList<UcumAtom> UsedAtoms,
        IReadOnlyList<string> Annotations);

    private sealed class TokenReader(IReadOnlyList<UcumToken> tokens)
    {
        private int _index;

        public UcumToken Current => tokens[_index];

        public void Advance()
        {
            if (_index < tokens.Count - 1)
                _index++;
        }
    }
}
