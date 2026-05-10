using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.LanguageServer.Handlers;

internal sealed class SignatureHelpHandler : ISignatureHelpHandler
{
    private static readonly Container<string> TriggerCharacters = new("(", ",");
    private static readonly Container<string> RetriggerCharacters = new(",");

    private readonly DocumentStore _store;

    public SignatureHelpHandler(DocumentStore store)
    {
        _store = store;
    }

    public SignatureHelpRegistrationOptions GetRegistrationOptions(SignatureHelpCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
            TriggerCharacters = TriggerCharacters,
            RetriggerCharacters = RetriggerCharacters,
        };

    public Task<SignatureHelp?> Handle(SignatureHelpParams request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
        {
            return Task.FromResult<SignatureHelp?>(null);
        }

        return Task.FromResult(CreateSignatureHelp(state.Current, request.Position));
    }

    internal static SignatureHelp? CreateSignatureHelp(Compilation compilation, Position position)
    {
        if (!CallContextResolver.TryFindActiveCall(compilation, position, out var call))
        {
            return null;
        }

        var signatures = call.IsAccessor
            ? CreateAccessorSignatures(call)
            : CreateFunctionSignatures(call.Name);

        if (signatures.Length == 0)
        {
            return null;
        }

        var maxParameterCount = signatures.Max(signature => signature.ParameterCount);
        var activeParameter = maxParameterCount == 0
            ? 0
            : Math.Min(call.ActiveParameter, maxParameterCount - 1);

        return new SignatureHelp
        {
            Signatures = new Container<SignatureInformation>(signatures.Select(signature => signature.Information)),
            ActiveSignature = SelectActiveSignature(signatures, activeParameter),
            ActiveParameter = activeParameter,
        };
    }

    internal static bool IsMethodLikeAccessor(TypeAccessor accessor) =>
        accessor.ParameterType is not null || accessor is ElementParameterAccessor;

    private static ImmutableArray<SignatureEntry> CreateFunctionSignatures(string name)
    {
        var signatures = ImmutableArray.CreateBuilder<SignatureEntry>();
        foreach (var meta in Functions.FindByName(name))
        {
            var parameters = BuildFunctionParameters(meta);
            var label = $"{CompletionHandler.GetFunctionLabel(meta)}({string.Join(", ", parameters.Select(parameter => parameter.DisplayLabel))})";

            signatures.Add(new SignatureEntry(
                new SignatureInformation
                {
                    Label = label,
                    Documentation = new StringOrMarkupContent(meta.HoverDescription ?? meta.Description),
                    Parameters = new Container<ParameterInformation>(parameters.Select(parameter => parameter.Information)),
                },
                parameters.Length));
        }

        return signatures.ToImmutable();
    }

    private static ImmutableArray<SignatureEntry> CreateAccessorSignatures(ActiveCallContext call)
    {
        if (call.ReceiverType is null)
        {
            return [];
        }

        var accessor = Types.GetMeta(call.ReceiverType.Value).Accessors.FirstOrDefault(candidate =>
            string.Equals(CompletionHandler.GetAccessorLabel(candidate), call.Name, StringComparison.Ordinal)
            && IsMethodLikeAccessor(candidate));

        if (accessor is null)
        {
            return [];
        }

        var parameters = BuildAccessorParameters(accessor);
        var label = $"{CompletionHandler.GetAccessorLabel(accessor)}({string.Join(", ", parameters.Select(parameter => parameter.DisplayLabel))})";

        return
        [
            new SignatureEntry(
                new SignatureInformation
                {
                    Label = label,
                    Documentation = new StringOrMarkupContent(accessor.Description),
                    Parameters = new Container<ParameterInformation>(parameters.Select(parameter => parameter.Information)),
                },
                parameters.Length),
        ];
    }

    private static ImmutableArray<ParameterEntry> BuildFunctionParameters(FunctionMeta meta)
    {
        var maxParameterCount = meta.Overloads.Max(overload => overload.Parameters.Count);
        var parameters = ImmutableArray.CreateBuilder<ParameterEntry>(maxParameterCount);

        for (var index = 0; index < maxParameterCount; index++)
        {
            var slotParameters = meta.Overloads
                .Where(overload => overload.Parameters.Count > index)
                .Select(overload => overload.Parameters[index])
                .ToArray();

            var parameterName = slotParameters
                .Select(parameter => parameter.Name)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
                ?? $"arg{index + 1}";

            var typeKinds = slotParameters
                .Select(parameter => parameter.Kind)
                .Distinct()
                .ToArray();

            var typeDisplay = string.Join(" | ", typeKinds.Select(GetTypeLabel));
            var label = $"{parameterName} as {typeDisplay}";

            parameters.Add(new ParameterEntry(
                label,
                new ParameterInformation
                {
                    Label = new ParameterInformationLabel(label),
                    Documentation = new StringOrMarkupContent(string.Join(Environment.NewLine, typeKinds.Select(GetTypeDescription))),
                }));
        }

        return parameters.ToImmutable();
    }

    private static ImmutableArray<ParameterEntry> BuildAccessorParameters(TypeAccessor accessor)
    {
        if (accessor.ParameterType is { } parameterType)
        {
            var label = GetTypeLabel(parameterType);
            return
            [
                new ParameterEntry(
                    label,
                    new ParameterInformation
                    {
                        Label = new ParameterInformationLabel(label),
                        Documentation = new StringOrMarkupContent(GetTypeDescription(parameterType)),
                    }),
            ];
        }

        if (accessor is ElementParameterAccessor)
        {
            const string label = "value";
            return
            [
                new ParameterEntry(
                    label,
                    new ParameterInformation
                    {
                        Label = new ParameterInformationLabel(label),
                        Documentation = new StringOrMarkupContent("Element value from the receiver collection."),
                    }),
            ];
        }

        return [];
    }

    private static int SelectActiveSignature(ImmutableArray<SignatureEntry> signatures, int activeParameter)
    {
        for (var index = 0; index < signatures.Length; index++)
        {
            if (activeParameter < signatures[index].ParameterCount)
            {
                return index;
            }
        }

        return Math.Max(signatures.Length - 1, 0);
    }

    private static string GetTypeLabel(TypeKind kind) => Types.GetMeta(kind).DisplayName;

    private static string GetTypeDescription(TypeKind kind)
    {
        var meta = Types.GetMeta(kind);
        return $"{meta.DisplayName}: {meta.Description}";
    }

    private readonly record struct SignatureEntry(SignatureInformation Information, int ParameterCount);

    private readonly record struct ParameterEntry(string DisplayLabel, ParameterInformation Information);
}
