using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept;
using Precept.LanguageServer;
using Precept.LanguageServer.Handlers;
using Precept.Pipeline;
using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Precept.LanguageServer.Tests;

internal static class SymbolNavigationHandlerTestHelpers
{
    internal static readonly DocumentUri Uri = DocumentUri.FromFileSystemPath(@"C:\symbol-navigation-test.precept");

    internal static Compilation Compile(string source)
    {
        var compilation = Compiler.Compile(source);
        compilation.HasErrors.Should().BeFalse("Slice 20 fixtures must compile cleanly");
        return compilation;
    }

    internal static Position PositionAt(SourceSpan span) =>
        new(span.StartLine - 1, span.StartColumn - 1);

    internal static LspRange[] FieldRanges(Compilation compilation, string fieldName) =>
        [DiagnosticProjector.ToRange(compilation.Semantics.Fields.Single(field => field.Name == fieldName).NameSpan),
         .. compilation.Semantics.FieldReferences
             .Where(reference => reference.Field.Name == fieldName)
             .Select(reference => DiagnosticProjector.ToRange(reference.Site))];

    internal static LspRange[] StateRanges(Compilation compilation, string stateName) =>
        [DiagnosticProjector.ToRange(compilation.Semantics.States.Single(state => state.Name == stateName).NameSpan),
         .. compilation.Semantics.StateReferences
             .Where(reference => reference.State.Name == stateName)
             .Select(reference => DiagnosticProjector.ToRange(reference.Site))];

    internal static LspRange[] EventRanges(Compilation compilation, string eventName) =>
        [DiagnosticProjector.ToRange(compilation.Semantics.Events.Single(@event => @event.Name == eventName).NameSpan),
         .. compilation.Semantics.EventReferences
             .Where(reference => reference.Event.Name == eventName)
             .Select(reference => DiagnosticProjector.ToRange(reference.Site))];

    internal static LspRange[] ArgRanges(Compilation compilation, string eventName, string argName) =>
        [DiagnosticProjector.ToRange(compilation.Semantics.Events
             .Single(@event => @event.Name == eventName)
             .Args
             .Single(arg => arg.Name == argName)
             .Span),
         .. compilation.Semantics.ArgReferences
              .Where(reference => reference.Arg.EventName == eventName && reference.Arg.Name == argName)
              .Select(reference => DiagnosticProjector.ToRange(reference.Site))];

    internal static LspRange[] ArgIdentifierRanges(Compilation compilation, string eventName, string argName) =>
        [ToIdentifierRange(compilation.Semantics.Events
             .Single(@event => @event.Name == eventName)
             .Args
             .Single(arg => arg.Name == argName)),
         .. compilation.Semantics.ArgReferences
              .Where(reference => reference.Arg.EventName == eventName && reference.Arg.Name == argName)
              .Select(ToIdentifierRange)];

    internal static async Task<TResult> InvokeHandlerAsync<TResult>(string handlerTypeName, object request, Compilation compilation)
    {
        var store = new DocumentStore();
        store.GetOrAdd(Uri).Update(compilation);

        var handlerType = typeof(DefinitionHandler).Assembly.GetType(handlerTypeName);
        handlerType.Should().NotBeNull($"{handlerTypeName} must exist for Slice 20");

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
        if (result is null)
        {
            return default!;
        }

        return result.Should().BeAssignableTo<TResult>().Subject;
    }

    private static LspRange ToIdentifierRange(TypedArg arg)
    {
        var span = arg.Span.Length == arg.Name.Length
            ? arg.Span
            : new SourceSpan(
                arg.Span.Offset,
                arg.Name.Length,
                arg.Span.StartLine,
                arg.Span.StartColumn,
                arg.Span.StartLine,
                arg.Span.StartColumn + arg.Name.Length);

        return DiagnosticProjector.ToRange(span);
    }

    private static LspRange ToIdentifierRange(ArgReference reference)
    {
        var span = reference.Site.Length == reference.Arg.Name.Length
            ? reference.Site
            : new SourceSpan(
                reference.Site.End - reference.Arg.Name.Length,
                reference.Arg.Name.Length,
                reference.Site.EndLine,
                reference.Site.EndColumn - reference.Arg.Name.Length,
                reference.Site.EndLine,
                reference.Site.EndColumn);

        return DiagnosticProjector.ToRange(span);
    }
}
