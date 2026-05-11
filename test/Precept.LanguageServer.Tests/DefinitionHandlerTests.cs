using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer.Handlers;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class DefinitionHandlerTests
{
    private static Location GetSingleLocation(LocationOrLocationLinks locations)
    {
        var target = locations.Single();
        target.IsLocation.Should().BeTrue();
        target.Location.Should().NotBeNull();
        return target.Location!;
    }

    private const string Source = """
precept OrderItem
field Quantity as number
field Total as number <- Quantity
state Pending initial
state Active
event Activate(Reason as string)
from Pending on Activate
    -> transition Active
on Activate ensure Activate.Reason != \"\" because \"Reason is required\"
""";

    private static readonly DocumentUri Uri = DocumentUri.FromFileSystemPath(@"C:\definition-handler-test.precept");

    [Fact]
    public void HandleDefinition_OnFieldReference_ReturnsFieldDeclarationLocation()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var locations = DefinitionHandler.HandleDefinition(Uri, compilation, new Position(2, 25));
        var location = GetSingleLocation(locations);

        location.Uri.Should().Be(Uri);
        location.Range.Should().BeEquivalentTo(DiagnosticProjector.ToRange(compilation.Semantics.Fields.Single(field => field.Name == "Quantity").NameSpan));
    }

    [Fact]
    public void HandleDefinition_OnStateReference_ReturnsStateDeclarationLocation()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var locations = DefinitionHandler.HandleDefinition(Uri, compilation, new Position(6, 5));
        var location = GetSingleLocation(locations);

        location.Uri.Should().Be(Uri);
        location.Range.Should().BeEquivalentTo(DiagnosticProjector.ToRange(compilation.Semantics.States.Single(state => state.Name == "Pending").NameSpan));
    }

    [Fact]
    public void HandleDefinition_OnEventReference_ReturnsEventDeclarationLocation()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var locations = DefinitionHandler.HandleDefinition(Uri, compilation, new Position(6, 16));
        var location = GetSingleLocation(locations);

        location.Uri.Should().Be(Uri);
        location.Range.Should().BeEquivalentTo(DiagnosticProjector.ToRange(compilation.Semantics.Events.Single(evt => evt.Name == "Activate").NameSpan));
    }

    [Fact]
    public void HandleDefinition_OnArgumentReference_ReturnsArgumentDeclarationLocation()
    {
        var compilation = Precept.Compiler.Compile(Source);
        var argReference = compilation.Semantics.ArgReferences.Single(reference => reference.Arg.Name == "Reason");

        var locations = DefinitionHandler.HandleDefinition(
            Uri,
            compilation,
            new Position(argReference.Site.StartLine - 1, argReference.Site.StartColumn - 1));
        var location = GetSingleLocation(locations);
        var arg = compilation.Semantics.Events.Single().Args.Single();

        location.Uri.Should().Be(Uri);
        location.Range.Should().BeEquivalentTo(DiagnosticProjector.ToRange(arg.Span));
    }

    [Fact]
    public void HandleDefinition_NoReference_ReturnsEmpty()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var locations = DefinitionHandler.HandleDefinition(Uri, compilation, new Position(0, 0));

        locations.Should().BeEmpty();
    }
}
