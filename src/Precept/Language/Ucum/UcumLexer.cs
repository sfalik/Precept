namespace Precept.Language;

public static class UcumLexer
{
    public static IReadOnlyList<UcumToken> Tokenize(string expression)
    {
        var tokens = new List<UcumToken>();
        var index = 0;

        while (index < expression.Length)
        {
            var current = expression[index];
            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            switch (current)
            {
                case '.':
                    tokens.Add(new UcumToken(UcumTokenKind.Dot, ".", index, 1));
                    index++;
                    continue;
                case '/':
                    tokens.Add(new UcumToken(UcumTokenKind.Slash, "/", index, 1));
                    index++;
                    continue;
                case '(':
                    tokens.Add(new UcumToken(UcumTokenKind.OpenParen, "(", index, 1));
                    index++;
                    continue;
                case ')':
                    tokens.Add(new UcumToken(UcumTokenKind.CloseParen, ")", index, 1));
                    index++;
                    continue;
                case '^':
                {
                    var exponentStart = index;
                    index++;
                    if (index < expression.Length && (expression[index] == '+' || expression[index] == '-'))
                        index++;

                    var digitsStart = index;
                    while (index < expression.Length && char.IsDigit(expression[index]))
                        index++;

                    if (digitsStart == index)
                    {
                        tokens.Add(new UcumToken(UcumTokenKind.Error, "^", exponentStart, 1));
                        continue;
                    }

                    tokens.Add(new UcumToken(UcumTokenKind.Exponent, expression[(exponentStart + 1)..index], exponentStart, index - exponentStart));
                    continue;
                }
                case '{':
                {
                    var end = expression.IndexOf('}', index + 1);
                    if (end < 0)
                    {
                        tokens.Add(new UcumToken(UcumTokenKind.Error, expression[index..], index, expression.Length - index));
                        index = expression.Length;
                        continue;
                    }

                    tokens.Add(new UcumToken(UcumTokenKind.Annotation, expression[(index + 1)..end], index, end - index + 1));
                    index = end + 1;
                    continue;
                }
            }

            var start = index;
            while (index < expression.Length && !char.IsWhiteSpace(expression[index]) && "./()^{}".IndexOf(expression[index]) < 0)
                index++;

            tokens.Add(new UcumToken(UcumTokenKind.Atom, expression[start..index], start, index - start));
        }

        tokens.Add(new UcumToken(UcumTokenKind.EndOfInput, string.Empty, expression.Length, 0));
        return tokens;
    }
}
