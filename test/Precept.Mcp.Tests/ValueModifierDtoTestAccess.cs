using System.Collections.Generic;
using FluentAssertions;
using Precept.Language;

namespace Precept.Mcp.Tests;

internal static class ValueModifierDtoTestAccess
{
    public static IReadOnlyList<dynamic> GetEntries(object modifierCatalog)
    {
        var bucket = modifierCatalog.GetType().GetProperty("Value")
            ?? modifierCatalog.GetType().GetProperty("Field");

        bucket.Should().NotBeNull("modifier DTOs should expose a value-modifier bucket");

        return ((System.Collections.IEnumerable)bucket!.GetValue(modifierCatalog)!)
            .Cast<dynamic>()
            .ToArray();
    }

    public static IReadOnlyList<dynamic> GetCatalogMetas()
        => Modifiers.All
            .Where(meta => meta.GetType().Name is "ValueModifierMeta")
            .Cast<dynamic>()
            .ToArray();

    public static string[] GetKinds(object modifierCatalog)
        => GetEntries(modifierCatalog)
            .Select(entry => (string)entry.Kind)
            .Cast<string>()
            .ToArray();

    public static string[] GetCatalogKinds()
        => GetCatalogMetas()
            .Select(meta => meta.Kind.ToString())
            .Cast<string>()
            .ToArray();

    public static IReadOnlyList<object> GetApplicableTypes(object entry)
        => ((System.Collections.IEnumerable)GetProperty<object>(entry, "ApplicableTypes"))
            .Cast<object>()
            .ToArray();

    public static string[] GetApplicableDeclarationSites(object entry)
        => ((System.Collections.IEnumerable)GetProperty<object>(entry, "ApplicableDeclarationSites"))
            .Cast<object>()
            .Select(value => value.ToString()!)
            .ToArray();

    public static string[] GetProofSatisfactions(object entry)
        => ((System.Collections.IEnumerable)GetProperty<object>(entry, "ProofSatisfactions"))
            .Cast<object>()
            .Select(value => value.ToString()!)
            .ToArray();

    public static T GetProperty<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName);
        property.Should().NotBeNull($"{instance.GetType().Name} should expose {propertyName}");
        return (T)property!.GetValue(instance)!;
    }

    public static dynamic GetCatalogMeta(ModifierKind kind)
        => GetCatalogMetas().Single(meta => meta.Kind == kind);
}
