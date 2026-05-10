using System;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer.Handlers;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class SignatureHelpHandlerTests
{
    [Fact]
    public void SignatureHelp_Round_ShowsBothOverloads()
    {
        var help = GetSignatureHelp("""
            precept LoanApplication
            field Rounded as integer default round(¦1)
            state Draft initial
            """);

        help.Should().NotBeNull();
        help!.Signatures.Should().HaveCount(2);
        help.ActiveSignature.Should().Be(0);
        help.Signatures.Select(signature => signature.Label).Should().OnlyContain(label => label.StartsWith("round(", StringComparison.Ordinal));
        help.Signatures.Select(signature => signature.Label).Should().Contain(label => label.Contains("places", StringComparison.Ordinal));
    }

    [Fact]
    public void SignatureHelp_StartsWith_ShowsNamedParameters()
    {
        var help = GetSignatureHelp("""
            precept LoanApplication
            field Email as string
            state Draft initial
            event Submit
            from Draft on Submit when startsWith(Email, ¦"info@")
                -> no transition
            """);

        help.Should().NotBeNull();
        help!.ActiveParameter.Should().Be(1);
        help.Signatures.Should().ContainSingle();

        var signature = help.Signatures.Single();
        signature.Label.Should().Contain("startsWith(str as string, prefix as string)");
        signature.Parameters.Should().HaveCount(2);
        signature.Parameters.Select(parameter => parameter.Label.Label).Should().Equal("str as string", "prefix as string");
    }

    [Fact]
    public void SignatureHelp_AccessorCall_ShowsAccessorSignature()
    {
        var help = GetSignatureHelp("""
            precept IncidentReport
            field CrewLog as list of string
            state Draft initial
            event Dispatch
            from Draft on Dispatch when CrewLog.at(¦0) is set
                -> no transition
            """);

        help.Should().NotBeNull();
        help!.Signatures.Should().ContainSingle();
        help.ActiveParameter.Should().Be(0);

        var signature = help.Signatures.Single();
        signature.Label.Should().Be("at(integer)");
        signature.Documentation.Should().NotBeNull();
        signature.Documentation!.String.Should().ContainEquivalentOf("zero-based position");
        signature.Parameters.Should().ContainSingle();
        signature.Parameters.Single().Label.Label.Should().Be("integer");
    }

    [Fact]
    public void SignatureHelp_NoActiveCall_ReturnsNull()
    {
        var help = GetSignatureHelp("""
            precept LoanApplication
            field Email as string
            state Draft initial
            event Submit
            from Draft on Submit when ¦Email is set
                -> no transition
            """);

        help.Should().BeNull();
    }

    private static SignatureHelp? GetSignatureHelp(string sourceWithCursor)
    {
        var position = GetCursorPosition(sourceWithCursor);
        var source = sourceWithCursor.Replace(CursorMarker, string.Empty, StringComparison.Ordinal);
        var compilation = Precept.Compiler.Compile(source);
        return SignatureHelpHandler.CreateSignatureHelp(compilation, position);
    }

    private static Position GetCursorPosition(string sourceWithCursor)
    {
        var markerIndex = sourceWithCursor.IndexOf(CursorMarker, StringComparison.Ordinal);
        markerIndex.Should().BeGreaterThanOrEqualTo(0, "each test source must include a cursor marker");

        var line = 0;
        var character = 0;
        for (var index = 0; index < markerIndex; index++)
        {
            if (sourceWithCursor[index] == '\n')
            {
                line++;
                character = 0;
            }
            else if (sourceWithCursor[index] != '\r')
            {
                character++;
            }
        }

        return new Position(line, character);
    }

    private const string CursorMarker = "¦";
}
