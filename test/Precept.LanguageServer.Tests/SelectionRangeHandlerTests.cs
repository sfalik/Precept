using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.LanguageServer;
using Precept.LanguageServer.Handlers;
using Precept.Pipeline;
using Xunit;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Precept.LanguageServer.Tests;

public class SelectionRangeHandlerTests
{
    private const string Source = """
        precept RestaurantWaitlist
        field CurrentParty as string optional
        state Accepting initial
        event SeatNextParty
        from Accepting on SeatNextParty when CurrentParty != ""
            -> no transition
        """;

    private static readonly DocumentUri Uri = DocumentUri.FromFileSystemPath(@"C:\selection-range-handler-test.precept");

    [Fact]
    public async Task SelectionRange_Identifier_ExpandsToExpressionThenConstruct()
    {
        var compilation = Compile(Source);
        var (construct, guardSlot, guardExpression) = GetGuardChain(compilation);
        var identifierToken = compilation.Tokens.Tokens.Single(token =>
            token.Kind == TokenKind.Identifier
            && token.Text == "CurrentParty"
            && guardExpression.Span.Offset <= token.Span.Offset
            && token.Span.End <= guardExpression.Span.End);

        var ranges = await RequestSelectionRangesAsync(
            compilation,
            PositionAt(identifierToken.Span));

        var returnedRanges = ranges.ToArray();
        returnedRanges.Should().HaveCount(1);
        ExpandChain(returnedRanges[0]).Should().Equal(
            DiagnosticProjector.ToRange(identifierToken.Span),
            DiagnosticProjector.ToRange(guardExpression.Span),
            DiagnosticProjector.ToRange(guardSlot.Span),
            DiagnosticProjector.ToRange(construct.Span));
    }

    [Fact]
    public async Task SelectionRange_MultiplePositions_ReturnAlignedChains()
    {
        var compilation = Compile(Source);
        var (construct, guardSlot, guardExpression) = GetGuardChain(compilation);
        var binary = guardExpression.Should().BeOfType<BinaryOperationExpression>().Subject;
        var identifierToken = GetTokenAt(compilation, PositionAt(binary.Left.Span));
        var literalToken = GetTokenAt(compilation, PositionAt(binary.Right.Span));

        var ranges = await RequestSelectionRangesAsync(
            compilation,
            PositionAt(binary.Right.Span),
            PositionAt(binary.Left.Span));

        var returnedRanges = ranges.ToArray();
        returnedRanges.Should().HaveCount(2);
        ExpandChain(returnedRanges[0]).Should().Equal(
            DiagnosticProjector.ToRange(literalToken.Span),
            DiagnosticProjector.ToRange(guardExpression.Span),
            DiagnosticProjector.ToRange(guardSlot.Span),
            DiagnosticProjector.ToRange(construct.Span));
        ExpandChain(returnedRanges[1]).Should().Equal(
            DiagnosticProjector.ToRange(identifierToken.Span),
            DiagnosticProjector.ToRange(guardExpression.Span),
            DiagnosticProjector.ToRange(guardSlot.Span),
            DiagnosticProjector.ToRange(construct.Span));
    }

    private static Compilation Compile(string source)
    {
        var compilation = Precept.Compiler.Compile(source);
        compilation.HasErrors.Should().BeFalse("Slice 25 fixtures must compile cleanly");
        return compilation;
    }

    private static async Task<Container<SelectionRange>> RequestSelectionRangesAsync(Compilation compilation, params Position[] positions)
    {
        var request = new SelectionRangeParams
        {
            TextDocument = new TextDocumentIdentifier(Uri),
            Positions = new Container<Position>(positions),
        };

        return await InvokeHandlerAsync<Container<SelectionRange>>(
            "Precept.LanguageServer.Handlers.SelectionRangeHandler",
            request,
            compilation);
    }

    private static async Task<TResult> InvokeHandlerAsync<TResult>(string handlerTypeName, object request, Compilation compilation)
    {
        var store = new DocumentStore();
        store.GetOrAdd(Uri).Update(compilation);

        var handlerType = typeof(DefinitionHandler).Assembly.GetType(handlerTypeName);
        handlerType.Should().NotBeNull($"{handlerTypeName} must exist for Slice 25");

        var constructor = handlerType!
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SingleOrDefault(ctor =>
            {
                var parameters = ctor.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType == typeof(DocumentStore);
            });

        constructor.Should().NotBeNull($"{handlerTypeName} must accept DocumentStore like the existing LS handlers");

        var handler = constructor!.Invoke([store]);
        var handleMethod = handlerType.GetMethod(
            "Handle",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: [request.GetType(), typeof(CancellationToken)],
            modifiers: null);

        handleMethod.Should().NotBeNull($"{handlerTypeName} must expose the standard Handle(request, cancellationToken) entrypoint");

        var taskObject = handleMethod!.Invoke(handler, [request, CancellationToken.None]);
        taskObject.Should().BeAssignableTo<Task>();

        var task = (Task)taskObject!;
        await task.ConfigureAwait(false);

        var result = task.GetType().GetProperty("Result")!.GetValue(task);
        result.Should().NotBeNull();
        return result.Should().BeAssignableTo<TResult>().Subject;
    }

    private static (ParsedConstruct Construct, GuardClauseSlot GuardSlot, ParsedExpression GuardExpression) GetGuardChain(Compilation compilation)
    {
        var construct = compilation.ConstructManifest.Constructs.Single(candidate => candidate.HasSlot<GuardClauseSlot>(ConstructSlotKind.GuardClause));
        var guardSlot = construct.GetRequiredSlot<GuardClauseSlot>(ConstructSlotKind.GuardClause);
        return (construct, guardSlot, guardSlot.Expression);
    }

    private static Token GetTokenAt(Compilation compilation, Position position) =>
        compilation.Tokens.Tokens.Single(token => Contains(token.Span, position));

    private static Position PositionAt(SourceSpan span) =>
        new(span.StartLine - 1, span.StartColumn - 1);

    private static LspRange[] ExpandChain(SelectionRange selectionRange)
    {
        var ranges = new List<LspRange>();
        for (var current = selectionRange; current is not null; current = current.Parent)
        {
            ranges.Add(current.Range);
        }

        return ranges.ToArray();
    }

    private static bool Contains(SourceSpan span, Position position)
    {
        var line = position.Line + 1;
        var character = position.Character + 1;

        if (line < span.StartLine || line > span.EndLine)
        {
            return false;
        }

        if (line == span.StartLine && character < span.StartColumn)
        {
            return false;
        }

        if (line == span.EndLine && character >= span.EndColumn)
        {
            return false;
        }

        return true;
    }
}
