using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Precept.Language;

namespace Precept.Tests;

internal sealed class ValueModifierHandle(ModifierMeta runtimeMeta)
{
    private readonly ModifierMeta _runtimeMeta = runtimeMeta;

    public object RuntimeInstance => _runtimeMeta;
    public ModifierKind Kind => _runtimeMeta.Kind;
    public TokenMeta Token => _runtimeMeta.Token;
    public string Description => _runtimeMeta.Description;
    public ModifierCategory Category => _runtimeMeta.Category;
    public TypeTarget[] ApplicableTo => Read<TypeTarget[]>("ApplicableTo");
    public bool HasValue => Read<bool>("HasValue");
    public ModifierKind? BoundCounterpart => Read<ModifierKind?>("BoundCounterpart");
    public ModifierKind[] Subsumes => Read<ModifierKind[]>("Subsumes");
    public ProofSatisfaction[] ProofSatisfactions => Read<ProofSatisfaction[]>("ProofSatisfactions");
    public string? HoverDescription => Read<string?>("HoverDescription");
    public string? UsageExample => Read<string?>("UsageExample");
    public string? SnippetTemplate => Read<string?>("SnippetTemplate");

    private T Read<T>(string propertyName)
    {
        var property = _runtimeMeta.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property.Should().NotBeNull($"{_runtimeMeta.GetType().Name} should expose {propertyName}");
        return (T)property!.GetValue(_runtimeMeta)!;
    }
}

internal static class ValueModifierTestAccess
{
    public static ValueModifierHandle GetMeta(ModifierKind kind)
    {
        var meta = Modifiers.GetMeta(kind);
        IsSupportedRuntimeName(meta.GetType().Name).Should().BeTrue(
            $"ModifierKind.{kind} should resolve to the value-modifier subtype");
        return new ValueModifierHandle(meta);
    }

    public static IReadOnlyList<ValueModifierHandle> All()
        => Modifiers.All
            .Where(meta => IsSupportedRuntimeName(meta.GetType().Name))
            .Select(meta => new ValueModifierHandle(meta))
            .ToArray();

    public static string RuntimeTypeName(ModifierKind kind)
        => Modifiers.GetMeta(kind).GetType().Name;

    public static PropertyInfo? FindDeclarationSiteProperty(ValueModifierHandle meta)
        => meta.RuntimeInstance.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(property =>
                property.PropertyType.IsEnum &&
                Enum.GetNames(property.PropertyType).Contains("FieldDeclaration") &&
                Enum.GetNames(property.PropertyType).Any(name => name is "EventArgument" or "EventArgDeclaration"));

    public static bool HasDeclarationSiteFlag(ValueModifierHandle meta, string flagName)
    {
        var property = FindDeclarationSiteProperty(meta);
        property.Should().NotBeNull("value modifiers should declare where they are legal");

        var value = property!.GetValue(meta.RuntimeInstance);
        value.Should().NotBeNull($"{property.Name} should be populated");

        Enum.GetNames(property.PropertyType).Should().Contain(flagName);
        var flag = Enum.Parse(property.PropertyType, flagName);

        return (Convert.ToUInt64(value) & Convert.ToUInt64(flag)) == Convert.ToUInt64(flag);
    }

    public static bool HasAnyDeclarationSiteFlag(ValueModifierHandle meta, params string[] flagNames)
        => flagNames.Any(flagName =>
        {
            var property = FindDeclarationSiteProperty(meta);
            if (property is null)
            {
                return false;
            }

            if (!Enum.GetNames(property.PropertyType).Contains(flagName))
            {
                return false;
            }

            var value = property.GetValue(meta.RuntimeInstance);
            if (value is null)
            {
                return false;
            }

            var flag = Enum.Parse(property.PropertyType, flagName);
            return (Convert.ToUInt64(value) & Convert.ToUInt64(flag)) == Convert.ToUInt64(flag);
        });

    private static bool IsSupportedRuntimeName(string runtimeTypeName)
        => runtimeTypeName is "ValueModifierMeta";
}
