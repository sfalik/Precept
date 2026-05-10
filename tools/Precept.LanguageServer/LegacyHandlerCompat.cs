using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Precept.LanguageServer;

/// <summary>
/// Temporary compatibility shims for the legacy v1 test surface.
/// Slice 0b removes the shim layer entirely.
/// </summary>
public static class LegacyHandlerCompat
{
    public static Task<PreceptPreviewResponse> Handle(
        this PreceptPreviewHandler handler,
        PreceptPreviewRequest request,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(request, cancellationToken);

    public static Task<CommandOrCodeActionContainer?> Handle(
        this PreceptCodeActionHandler handler,
        CodeActionParams request,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(request, cancellationToken);
}
