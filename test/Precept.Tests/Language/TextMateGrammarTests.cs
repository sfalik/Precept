using System;
using System.IO;
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace Precept.Tests.Language;

public sealed class TextMateGrammarTests
{
    private static readonly JsonObject Grammar = LoadGrammar();

    [Theory]
    [InlineData("fieldScalarDeclaration", "6")]
    [InlineData("fieldCollectionDeclaration", "6")]
    public void FieldDeclaration_AsKeyword_UsesDeclarationScope(string repositoryKey, string captureKey)
        => GetCapture(repositoryKey, captureKey)["name"]!.GetValue<string>()
            .Should().Be("keyword.declaration.precept",
                because: "'as' in field declarations is declaration structure, not the gold grammar scope");

    [Theory]
    [InlineData("fieldScalarDeclaration", "9")]
    [InlineData("fieldCollectionDeclaration", "13")]
    public void FieldDeclaration_DefaultModifier_OverridesGoldGrammarScope(string repositoryKey, string captureKey)
    {
        var patterns = GetCapture(repositoryKey, captureKey)["patterns"]!.AsArray();
        var defaultIndex = FindPatternIndex(patterns, "keyword.declaration.precept", "\\bdefault\\b");
        var grammarFallbackIndex = FindIncludeIndex(patterns, "#grammarKeywords");

        defaultIndex.Should().BeGreaterThanOrEqualTo(0,
            because: "field-declaration default modifiers must stay off the gold grammar keyword scope");
        grammarFallbackIndex.Should().BeGreaterThan(defaultIndex,
            because: "the default override must win before the generic gold grammar fallback runs");
    }

    [Fact]
    public void RuleDesugaringModifiers_UseConstraintScope()
        => Grammar["repository"]!
            .AsObject()["ruleDesugaringModifiers"]!
            .AsObject()["patterns"]!
            .AsArray()[0]!
            .AsObject()["name"]!
            .GetValue<string>()
            .Should().Be("keyword.other.constraint.precept");

    private static JsonObject LoadGrammar()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "tools", "Precept.VsCode", "syntaxes", "precept.tmLanguage.json"));

        return JsonNode.Parse(File.ReadAllText(path))!.AsObject();
    }

    private static JsonObject GetCapture(string repositoryKey, string captureKey)
    {
        var repository = Grammar["repository"]!.AsObject();
        var declaration = repository[repositoryKey]!.AsObject();
        var pattern = declaration["patterns"]!.AsArray()[0]!.AsObject();
        var captures = pattern["captures"]!.AsObject();
        return captures[captureKey]!.AsObject();
    }

    private static int FindPatternIndex(JsonArray patterns, string name, string match)
    {
        for (var i = 0; i < patterns.Count; i++)
        {
            if (patterns[i] is not JsonObject pattern)
            {
                continue;
            }

            if (pattern["name"]?.GetValue<string>() == name && pattern["match"]?.GetValue<string>() == match)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindIncludeIndex(JsonArray patterns, string include)
    {
        for (var i = 0; i < patterns.Count; i++)
        {
            if (patterns[i] is not JsonObject pattern)
            {
                continue;
            }

            if (pattern["include"]?.GetValue<string>() == include)
            {
                return i;
            }
        }

        return -1;
    }
}
