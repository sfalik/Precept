using System.Linq;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.Tests.Language.Ucum;

public class UcumLexerTests
{
    [Fact]
    public void Tokenize_HandlesAtomsOperatorsExponentsAndAnnotations()
    {
        var tokens = UcumLexer.Tokenize("kg.m/s^2{RBC}");

        tokens.Select(token => token.Kind).Should().ContainInOrder(
            UcumTokenKind.Atom,
            UcumTokenKind.Dot,
            UcumTokenKind.Atom,
            UcumTokenKind.Slash,
            UcumTokenKind.Atom,
            UcumTokenKind.Exponent,
            UcumTokenKind.Annotation,
            UcumTokenKind.EndOfInput);
    }

    [Fact]
    public void Tokenize_HandlesBracketedAtomsAndGrouping()
    {
        var tokens = UcumLexer.Tokenize("mmol/(L.min)");

        tokens.Select(token => token.Kind).Should().Contain(UcumTokenKind.OpenParen);
        tokens.Select(token => token.Kind).Should().Contain(UcumTokenKind.CloseParen);
        tokens.Should().Contain(token => token.Text == "mmol");
    }
}
