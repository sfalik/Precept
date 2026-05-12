using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Precept.Language;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class ExtensionManifestTests
{
    [Fact]
    public void LanguageConfiguration_TypedConstantSingleQuote_AutoCloses()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GetLanguageConfigurationPath()));
        var root = document.RootElement;

        root.GetProperty("comments").GetProperty("lineComment").GetString().Should().Be("#");

        var autoClosingPairs = root.GetProperty("autoClosingPairs").EnumerateArray().ToArray();
        autoClosingPairs.Should().Contain(pair =>
            pair.GetProperty("open").GetString() == "'" &&
            pair.GetProperty("close").GetString() == "'");

        var surroundingPairs = root.GetProperty("surroundingPairs").EnumerateArray().ToArray();
        surroundingPairs.Should().Contain(pair =>
            pair.GetProperty("open").GetString() == "'" &&
            pair.GetProperty("close").GetString() == "'");
    }

    [Fact]
    public void PackageManifest_Activates_WhenAPreceptDocumentOpens()
    {
        var packageManifest = GetPackageManifest();
        var languages = packageManifest
            .GetProperty("contributes")
            .GetProperty("languages")
            .EnumerateArray()
            .ToArray();
        var activationEvents = packageManifest
            .GetProperty("activationEvents")
            .EnumerateArray()
            .Select(static item => item.GetString())
            .ToArray();

        languages.Should().Contain(language => language.GetProperty("id").GetString() == "precept");
        activationEvents.Should().Contain("workspaceContains:**/*.precept");
    }

    [Fact]
    public void PackageManifest_GrammarKeywords_UseGrammarColorInsteadOfGold()
    {
        var settings = GetTextMateRuleSettings("keyword.other.grammar.precept");

        settings.GetProperty("foreground").GetString().Should().Be("#6366F1");
    }

    [Fact]
    public void PackageManifest_MessageStrings_RemainGold()
    {
        var settings = GetTextMateRuleSettings("string.quoted.double.message.precept");

        settings.GetProperty("foreground").GetString().Should().Be("#FBBF24");
    }

    [Fact]
    public void PackageManifest_ShowLanguageServerModeCommand_RemainsContributed()
    {
        var commands = GetPackageManifest()
            .GetProperty("contributes")
            .GetProperty("commands")
            .EnumerateArray()
            .ToArray();

        commands.Should().Contain(command =>
            command.GetProperty("command").GetString() == "precept.showLanguageServerMode" &&
            command.GetProperty("title").GetString() == "Show Language Server Mode" &&
            command.GetProperty("icon").GetString() == "$(server-process)");
    }

    [Fact]
    public void PackageManifest_SemanticTokenFallbackScopes_AlignWithGrammarScopes()
    {
        SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.Name).TextMateScope.Should().Be("entity.name.type.precept.precept");
        SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.State).TextMateScope.Should().Be("entity.name.type.state.precept");
        SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.Event).TextMateScope.Should().Be("entity.name.function.event.precept");
        SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.ArgName).TextMateScope.Should().Be("variable.parameter.precept");

        GetSemanticTokenScopes("preceptName").Should().Equal(SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.Name).TextMateScope);
        GetSemanticTokenScopes("preceptState").Should().Equal(SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.State).TextMateScope);
        GetSemanticTokenScopes("preceptState.preceptConstrained").Should().Equal("entity.name.type.state.constrained.precept");
        GetSemanticTokenScopes("preceptEvent").Should().Equal(SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.Event).TextMateScope);
        GetSemanticTokenScopes("preceptArgName").Should().Equal(SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.ArgName).TextMateScope);
    }

    [Fact]
    public void ExtensionSource_Activate_CreatesAndShowsLanguageServerStatusItem()
    {
        var activateBody = GetExtensionFunctionBody("export async function activate");

        activateBody.Should().MatchRegex(
            @"languageServerStatusItem\s*=\s*vscode\.window\.createStatusBarItem\(vscode\.StatusBarAlignment\.Left,\s*100\);");
        activateBody.Should().Contain("languageServerStatusItem.name = \"Precept Language Server\";");
        activateBody.Should().Contain("languageServerStatusItem.command = \"precept.showLanguageServerMode\";");
        activateBody.Should().Contain("context.subscriptions.push(languageServerStatusItem);");
        activateBody.Should().Contain("updateLanguageServerStatusItem(\"starting\", undefined, undefined);");
        activateBody.Should().Contain("languageServerStatusItem.show();");
        activateBody.Should().Contain(
            "const showLanguageServerModeDisposable = vscode.commands.registerCommand(\"precept.showLanguageServerMode\", () => {");
    }

    [Fact]
    public void ExtensionSource_StatusItemUpdate_ReShowsVisibleStatusAcrossServerStates()
    {
        var updateBody = GetExtensionFunctionBody("function updateLanguageServerStatusItem");

        updateBody.Should().Contain("case \"starting\":");
        updateBody.Should().Contain("languageServerStatusItem.text = `$(sync~spin) Precept${modeIcon}`;");
        updateBody.Should().Contain("case \"restarting\":");
        updateBody.Should().Contain("case \"error\":");
        updateBody.Should().Contain("languageServerStatusItem.text = `$(error) Precept${modeIcon}`;");
        updateBody.Should().Contain("case \"stopped\":");
        updateBody.Should().Contain("languageServerStatusItem.text = `$(circle-slash) Precept${modeIcon}`;");
        updateBody.Should().Contain("languageServerStatusItem.text = `$(pulse) Precept${modeIcon}${capCountLabel}`;");
        updateBody.Should().Contain("languageServerStatusItem.tooltip = new vscode.MarkdownString(tooltipText);");
        updateBody.Should().MatchRegex(@"languageServerStatusItem\.show\(\);\s*\}$");
    }

    private static string GetLanguageConfigurationPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tools", "Precept.VsCode", "language-configuration.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate tools\\Precept.VsCode\\language-configuration.json from test base directory.");
    }

    private static JsonElement GetTextMateRuleSettings(string scope)
    {
        var rules = GetPackageManifest()
            .GetProperty("contributes")
            .GetProperty("configurationDefaults")
            .GetProperty("editor.tokenColorCustomizations")
            .GetProperty("[*]")
            .GetProperty("textMateRules")
            .EnumerateArray()
            .ToArray();

        return rules
            .Single(rule => rule.GetProperty("scope").GetString() == scope)
            .GetProperty("settings")
            .Clone();
    }

    private static string[] GetSemanticTokenScopes(string tokenType) =>
        GetPackageManifest()
            .GetProperty("contributes")
            .GetProperty("semanticTokenScopes")
            .EnumerateArray()
            .Single()
            .GetProperty("scopes")
            .GetProperty(tokenType)
            .EnumerateArray()
            .Select(static scope => scope.GetString())
            .OfType<string>()
            .ToArray();

    private static JsonElement GetPackageManifest()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GetPackageManifestPath()));
        return document.RootElement.Clone();
    }

    private static string GetPackageManifestPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tools", "Precept.VsCode", "package.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate tools\\Precept.VsCode\\package.json from test base directory.");
    }

    private static string GetExtensionFunctionBody(string signature)
    {
        var source = File.ReadAllText(GetExtensionSourcePath());
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        if (signatureIndex < 0)
        {
            throw new InvalidOperationException($"Could not locate extension function signature '{signature}'.");
        }

        var bodyStart = source.IndexOf('{', signatureIndex);
        if (bodyStart < 0)
        {
            throw new InvalidOperationException($"Could not locate opening brace for extension function signature '{signature}'.");
        }

        var depth = 0;
        for (var index = bodyStart; index < source.Length; index++)
        {
            switch (source[index])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        return source.Substring(bodyStart, index - bodyStart + 1);
                    }

                    break;
            }
        }

        throw new InvalidOperationException($"Could not locate closing brace for extension function signature '{signature}'.");
    }

    private static string GetExtensionSourcePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tools", "Precept.VsCode", "src", "extension.ts");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate tools\\Precept.VsCode\\src\\extension.ts from test base directory.");
    }
}
