using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class ServerCapabilityTests
{
    [Fact]
    public async Task Initialize_AdvertisesFinalCapabilitySurface()
    {
        await using var host = await LspTestHost.CreateAsync();
        var capabilities = host.ServerCapabilities;

        capabilities.TextDocumentSync.Should().NotBeNull();
        capabilities.TextDocumentSync!.HasOptions.Should().BeTrue();
        var syncOptions = capabilities.TextDocumentSync.Options;
        syncOptions.Should().NotBeNull();
        syncOptions!.OpenClose.Should().BeTrue();
        syncOptions.Change.Should().Be(TextDocumentSyncKind.Full);
        capabilities.DiagnosticProvider.Should().BeNull("the language server pushes diagnostics on document sync instead of registering pull diagnostics");

        capabilities.SemanticTokensProvider.Should().NotBeNull();
        capabilities.SemanticTokensProvider!.Legend.TokenTypes.Should().Contain("preceptName");

        capabilities.CompletionProvider.Should().NotBeNull();
        capabilities.CompletionProvider!.TriggerCharacters.Should().BeEquivalentTo([" ", "'", ".", ">", "~"]);

        capabilities.HoverProvider.Should().NotBeNull();
        capabilities.HoverProvider!.RawValue.Should().NotBeNull();

        capabilities.DefinitionProvider.Should().NotBeNull();
        capabilities.DefinitionProvider!.RawValue.Should().NotBeNull();

        capabilities.DocumentSymbolProvider.Should().NotBeNull();
        capabilities.DocumentSymbolProvider!.RawValue.Should().NotBeNull();

        capabilities.FoldingRangeProvider.Should().NotBeNull();
        capabilities.FoldingRangeProvider!.RawValue.Should().NotBeNull();

        capabilities.CodeActionProvider.Should().NotBeNull();
        capabilities.CodeActionProvider!.IsValue.Should().BeTrue();
        capabilities.CodeActionProvider.Value!.CodeActionKinds.Should().Contain([CodeActionKind.QuickFix]);

        capabilities.ReferencesProvider.Should().NotBeNull();
        capabilities.ReferencesProvider!.RawValue.Should().NotBeNull();

        capabilities.DocumentHighlightProvider.Should().NotBeNull();
        capabilities.DocumentHighlightProvider!.RawValue.Should().NotBeNull();

        capabilities.RenameProvider.Should().NotBeNull();
        capabilities.RenameProvider!.RawValue.Should().NotBeNull();
        capabilities.SignatureHelpProvider.Should().NotBeNull();
        capabilities.SignatureHelpProvider!.TriggerCharacters.Should().BeEquivalentTo(["(", ","]);
        capabilities.SignatureHelpProvider.RetriggerCharacters.Should().BeEquivalentTo([","]);
        capabilities.WorkspaceSymbolProvider.Should().NotBeNull();
        capabilities.WorkspaceSymbolProvider!.RawValue.Should().NotBeNull();
        capabilities.SelectionRangeProvider.Should().NotBeNull();
        capabilities.SelectionRangeProvider!.RawValue.Should().NotBeNull();
    }
}
